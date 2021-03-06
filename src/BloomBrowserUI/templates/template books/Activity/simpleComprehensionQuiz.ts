// Note to future selves: In looking at BL-10037, Andrew and Gordon looked for some time to see how
// this file might be loaded by bloom-player. It isn't. The somewhat slimmed down file
// ".\src\activities\domActivities\SimpleCheckboxQuiz.ts" is included instead. In fact the solution
// for BL-10037 turned out to be an oversimplification of this code in that version.

// The js generated from this file is used in the template page generated from simpleComprehensionQuiz.pug.
// It makes sure the body element has the editMode class (if the editMode stylesheet is loaded)
// and installs appropriate click handlers (depending on edit mode) which manipulate the classes
// of .checkbox-and-textbox-choice elements to produce the desired checking and dimming of right
// and wrong answers. It also adds an appropriate class to answers that are empty (to hide them or
// dim them) and plays appropriate sounds when a right or wrong answer is chosen.
// Eventually it will cooperate with reader code to handle analytics.
// The output is also part of the bloom-player, which creates instances of the new simpleComprehensionQuiz
// pages dynamically in order to handle old-style comprehension questions represented as json,
// and needs the associated JS to make them work.

// Master function, called when document is ready, initializes CQ pages
function init(): void {
    ensureEditModeStyleSheet();
    initChoiceWidgets();
}

//------------ Code involved in setting the editMode class on the body element when appropriate-----
function ensureEditModeStyleSheet(): void {
    if (!inEditMode()) {
        return;
    }
    // with future Bloom versions, this might already be there,
    // but it doesn't matter if it is.
    document.body.classList.add("editMode");
}

function inEditMode(): boolean {
    for (let i = 0; i < document.styleSheets.length; i++) {
        const href = document.styleSheets[i].href;
        if (href && href.endsWith("editMode.css")) {
            return true;
        }
    }
    return false;
}

//------------ Code for managing the choice widgets-------

// Initialize the choice widgets, arranging for the appropriate click actions
// and for maintaining the class that indicates empty choice.
// Assumes the code that sets up the editMode class on the body element if appropriate has already been run.
function initChoiceWidgets(): void {
    // The value we store to indicate that at some point the user
    // chose this answer. We don't really need the value, because if the key for
    // that answer has a value, it will be this. But may as well
    // be consistent, in case we later add other options.
    // Unfortunately, when Typescript was upgraded, books with > 1 quiz page started reporting that
    // this constant was "redeclared", so we had to move the declaration inside each function that needed it.
    const kwasSelectedAtOnePoint = "wasSelectedAtOnePoint";
    markEmptyChoices();
    const observer = new MutationObserver(markEmptyChoices);
    observer.observe(document.body, { characterData: true, subtree: true });
    const list = document.getElementsByClassName("checkbox-and-textbox-choice");
    for (let i = 0; i < list.length; i++) {
        const x = list[i] as HTMLElement;
        const checkbox = getCheckBox(x);
        const correct = x.classList.contains("correct-answer");
        if (document.body.classList.contains("editMode")) {
            checkbox.addEventListener("click", handleEditModeClick);
            // Not sure why this doesn't get persisted along with the correct-answer class,
            // but glad it doesn't, because we don't want it to show up even as a flash
            // in reader mode.
            checkbox.checked = correct;
        } else {
            // There are likely to be several copies of this code, one for each quiz page, each doing this.
            // But we only need to do it once per element. In particular, we don't want multiple handlers
            // trying to play the same sound on each click. They can get out of sync and make a horrible noise.
            // But we only want to do it in read mode or it will get persisted and event handlers won't get set
            // up next time. See BL-7532.
            if (x.hasAttribute("data-simpleQuizInitComplete")) {
                continue;
            }
            x.setAttribute("data-simpleQuizInitComplete", "true");
            x.addEventListener("click", handleReadModeClick, { capture: true });
            const key = getStorageKeyForChoice(x! as HTMLElement);
            if (
                (window as any).BloomPlayer &&
                (window as any).BloomPlayer.getPageData(
                    x.closest(".bloom-page"),
                    key
                ) === kwasSelectedAtOnePoint
            ) {
                choiceWasClicked(x! as HTMLElement);
            } else {
                checkbox.checked = false; // just to make sure
            }
        }
    }
}

function getCheckBox(holder: HTMLElement): HTMLInputElement {
    return holder.firstElementChild as HTMLInputElement;
}

function handleEditModeClick(evt: Event): void {
    const target = evt.target as HTMLInputElement;
    if (!target) {
        return;
    }
    const wrapper = (evt.currentTarget as HTMLElement).parentElement;
    if (target.checked) {
        wrapper!.classList.add("correct-answer");
    } else {
        wrapper!.classList.remove("correct-answer");
    }
}

// Get a key for a checkbox. It only needs to be unique on this page.
// Enhance: If a new version of the book is downloaded with a different
// set of choices on this page, this sort of positional ID could select
// the wrong one. To fix this, we could make something generate a persistent
// id for a choice; but the author could still edit the text of the choice,
// making the stored choice invalid. Better: find a way to clear the relevant
// storage when downloading a new version of a book.
function getStorageKeyForChoice(choice: HTMLElement): string {
    const page = choice.closest(".bloom-page") as HTMLElement;

    // what is my index among the other choices on the page
    const choices = Array.from(
        page.getElementsByClassName("checkbox-and-textbox-choice")
    );
    const index = choices.indexOf(choice);
    const id = page.getAttribute("id");
    return "cbstate_" + index;
}

function handleReadModeClick(evt: Event): void {
    // (See declaration inside initChoiceWidgets().)
    const kwasSelectedAtOnePoint = "wasSelectedAtOnePoint";
    // prevent the browser messing with the check box checked state
    evt.stopPropagation();
    evt.preventDefault();
    const currentTarget = evt.currentTarget as HTMLElement;
    choiceWasClicked(currentTarget);
    const correct = currentTarget.classList.contains("correct-answer");
    const soundUrl = correct ? "right_answer.mp3" : "wrong_answer.mp3";
    playSound(soundUrl);
    // The ui shows items that were (selected but wrong) differently than
    // items that were never tried.
    const key = getStorageKeyForChoice(currentTarget);
    if ((window as any).BloomPlayer) {
        (window as any).BloomPlayer.storePageData(
            currentTarget.closest(".bloom-page"),
            key,
            kwasSelectedAtOnePoint
        );
    }

    reportScore(correct);
}

// it was either clicked just now, or we're loading from storage
// and we need to make it look like it looked last time we were on this
// page
function choiceWasClicked(choice: HTMLElement): void {
    // (See declaration inside initChoiceWidgets().)
    const kwasSelectedAtOnePoint = "wasSelectedAtOnePoint";
    const classes = choice.classList;
    classes.add(kwasSelectedAtOnePoint);
    // Make the state of the hidden input conform. Only if the
    // correct answer was clicked does the checkbox get checked.
    const checkBox = getCheckBox(choice);
    // at this point, we only actually make the check happen if
    // this was the correct answer
    if (checkBox) {
        const desiredState = classes.contains("correct-answer");
        checkBox.checked = desiredState;
        // Something I can't track down resets it to unchecked
        // if the user clicks on the input itself. Even with zero delay,
        // this makes something happen in the next event cycle that
        // keeps it the way we want.
        window.setTimeout(() => (checkBox.checked = desiredState), 0);
    }
}

function reportScore(correct: boolean): void {
    // Send the score and info for analytics.
    // Note: the Bloom Player is smart enough to only
    // record the analytics part the very first time we report a score for this page,
    // and only send it when it has been reported for all pages using the same
    // analyticsCategory as this page.
    // Note, it is up to the host of BloomPlayer whether it actually is sending the analytics to some server.
    if ((window as any).BloomPlayer) {
        (window as any).BloomPlayer.reportScoreForCurrentPage(
            1, // possible score on page
            correct ? 1 : 0, // actual score
            "comprehension"
        );
    }
}

function playSound(url: string): void {
    const player = getPagePlayer();
    player.setAttribute("src", url);
    player.play();
}

function getPagePlayer(): HTMLAudioElement {
    let player: HTMLAudioElement | null = document.querySelector(
        "#quiz-sound-player"
    ) as HTMLAudioElement;
    if (player && !player.play) {
        player.remove();
        player = null;
    }
    if (!player) {
        player = document.createElement("audio");
        player.setAttribute("id", "#quiz-sound-player");
        document.body.appendChild(player);
    }
    return player;
}

function markEmptyChoices(): void {
    const choices = document.getElementsByClassName(
        "checkbox-and-textbox-choice"
    );
    for (let i = 0; i < choices.length; i++) {
        if (hasVisibleContent(choices[i])) {
            choices[i].classList.remove("empty");
        } else {
            choices[i].classList.add("empty");
        }
    }
}

function hasVisibleContent(choice: Element): boolean {
    const editables = choice.getElementsByClassName("bloom-editable");

    return Array.from(editables).some(
        e =>
            e.classList.contains("bloom-visibility-code-on") &&
            (e.textContent || "").trim() !== ""
    );
}

//-------------- initialize -------------

// In some cases (loading into a bloom reader carousel, for example) the page may already be loaded.
if (document.readyState === "complete") {
    init();
} else {
    window.addEventListener("load", () => {
        init();
    });
}
