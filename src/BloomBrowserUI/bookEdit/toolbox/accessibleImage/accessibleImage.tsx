import * as React from "react";
import ToolboxToolReactAdaptor from "../toolboxToolReactAdaptor";
import { Div, Label } from "../../../react_components/l10n";
import { BloomApi } from "../../../utils/bloomApi";
import { ToolBox, ITool } from "../toolbox";
import { ApiBackedCheckbox } from "../../../react_components/apiBackedCheckbox";
import "./accessibleImage.less";
import { RequiresBloomEnterpriseWrapper } from "../../../react_components/requiresBloomEnterprise";
import { RadioGroup, Radio } from "../../../react_components/radio";
import { deuteranopia, tritanopia, achromatopsia } from "color-blind";

interface IState {
    kindOfColorBlindness: string;
}

// This react class implements the UI for the accessible images toolbox.
// Note: this file is included in toolboxBundle.js because webpack.config says to include all
// tsx files in bookEdit/toolbox.
// The toolbox is included in the list of tools because of the one line of immediately-executed code
// which  passes an instance of AccessibleImageToolAdaptor to ToolBox.registerTool();
export class AccessibleImageControls extends React.Component<{}, IState> {
    public readonly state: IState = {
        kindOfColorBlindness: "redGreen"
    };

    // This wants to be part of our state, passed as a prop to ApiBackedCheckbox.
    // But then we, and all the other clients of that class, have to be responsible
    // for interacting with the api to get and set that state. So, for the moment,
    // we just let the check box tell us what its value should be using onCheckChanged,
    // and use it to update the appearance of the page. Better solution wanted!
    private simulatingCataracts: boolean;
    private simulatingColorBlindness: boolean;

    public render() {
        return (
            <RequiresBloomEnterpriseWrapper>
                <div className="accessibleImageBody">
                    <Div l10nKey="EditTab.Toolbox.AccessibleImage.Overview">
                        You can use these check boxes to have Bloom simulate how
                        your images would look with various visual impairments.
                    </Div>
                    <ApiBackedCheckbox
                        className="checkBox"
                        apiEndpoint="accessibilityCheck/cataracts"
                        l10nKey="EditTab.Toolbox.AccessibleImage/Cataracts"
                        onCheckChanged={simulate =>
                            this.updateCataracts(simulate)
                        }
                    >
                        Cataracts
                    </ApiBackedCheckbox>
                    <ApiBackedCheckbox
                        className="checkBox colorBlindCheckBox"
                        apiEndpoint="accessibilityCheck/colorBlindness"
                        l10nKey="EditTab.Toolbox.AccessibleImage/ColorBlindness"
                        onCheckChanged={simulate =>
                            this.updateColorBlindnessCheck(simulate)
                        }
                    >
                        Color Blindness
                    </ApiBackedCheckbox>
                    <RadioGroup
                        onChange={val => this.updateColorBlindnessRadio(val)}
                        value={this.state.kindOfColorBlindness}
                    >
                        <Radio
                            l10nKey="EditTab.Toolbox.AccessibleImage.RedGreen"
                            value="RedGreen"
                        >
                            Red-Green
                        </Radio>
                        <Radio
                            l10nKey="EditTab.Toolbox.AccessibleImage.BlueYellow"
                            value="BlueYellow"
                        >
                            Blue-Yellow
                        </Radio>
                        <Radio
                            l10nKey="EditTab.Toolbox.AccessibleImage.Complete"
                            value="Complete"
                        >
                            Complete
                        </Radio>
                    </RadioGroup>
                </div>
            </RequiresBloomEnterpriseWrapper>
        );
    }

    private updateCataracts(simulate: boolean) {
        this.simulatingCataracts = simulate;
        this.updateSimulations();
    }

    private updateColorBlindnessCheck(simulate: boolean) {
        this.simulatingColorBlindness = simulate;
        this.updateSimulations();
    }

    private updateColorBlindnessRadio(mode: string) {
        BloomApi.postDataWithConfig(
            "accessibilityCheck/kindOfColorBlindness",
            mode,
            { headers: { "Content-Type": "application/json" } }
        );
        this.setState({ kindOfColorBlindness: mode });
        // componentDidUpdate will call updateSimulations when state is stable
    }

    public componentDidMount() {
        BloomApi.get("accessibilityCheck/kindOfColorBlindness", result => {
            this.setState({ kindOfColorBlindness: result.data });
        });
    }

    public componentDidUpdate(prevProps, prevState: IState) {
        this.updateSimulations();
    }

    public updateSimulations() {
        var page = ToolboxToolReactAdaptor.getPage();
        var body = page.ownerDocument.body;
        if (this.simulatingCataracts) {
            body.classList.add("simulateCataracts");
        } else {
            body.classList.remove("simulateCataracts");
        }
        AccessibleImageControls.removeColorBlindnessMarkup();
        if (this.simulatingColorBlindness) {
            body.classList.add("simulateColorBlindness");
            // For now limit it to these images because the positioning depends
            // on the img being the first thing in its parent and the parent
            // being positioned, which we can't count on for other images.
            var containers = page.getElementsByClassName(
                "bloom-imageContainer"
            );
            for (var i = 0; i < containers.length; i++) {
                var images = containers[i].getElementsByTagName("img");
                for (var imgIndex = 0; imgIndex < images.length; imgIndex++) {
                    this.makeColorBlindnessOverlay(images[imgIndex]);
                }
            }
        } else {
            body.classList.remove("simulateColorBlindness");
        }
    }

    public static removeAcessibleImageMarkup() {
        AccessibleImageControls.removeColorBlindnessMarkup();
        var page = ToolboxToolReactAdaptor.getPage();
        var body = page.ownerDocument.body;
        body.classList.remove("simulateColorBlindness");
        body.classList.remove("simulateCataracts");
    }

    private static removeColorBlindnessMarkup() {
        var page = ToolboxToolReactAdaptor.getPage();
        [].slice
            .call(page.getElementsByClassName("ui-cbOverlay"))
            .map(x => x.parentElement.removeChild(x));
    }

    private componentToHex(c) {
        var hex = c.toString(16);
        return hex.length == 1 ? "0" + hex : hex;
    }

    private rgbToHex(r, g, b) {
        return (
            "#" +
            this.componentToHex(r) +
            this.componentToHex(g) +
            this.componentToHex(b)
        );
    }

    private makeColorBlindnessOverlay(img: HTMLImageElement) {
        if (
            !img.complete ||
            img.naturalWidth === undefined ||
            img.naturalWidth === 0
        ) {
            // The image isn't loaded, so we can't make a color-blind simulation of it.
            // We could add an event listener for "loaded", but then we have to worry about
            // removing it again...just waiting a bit is simpler.
            window.setTimeout(() => this.makeColorBlindnessOverlay(img), 100);
            return;
        }
        var page = ToolboxToolReactAdaptor.getPage();
        var canvas = page.ownerDocument.createElement("canvas");
        // Make the canvas be the size the image is actually drawn.
        // Typically that means fewer pixels to calculate than doing the whole
        // image. To avoid distortion, we do have to make it the right shape.
        let canvasWidth = img.width;
        let canvasHeight = img.height;
        if (img.naturalWidth / img.naturalHeight < canvasWidth / canvasHeight) {
            // available space is too wide: make narrower
            canvasWidth = (canvasHeight * img.naturalWidth) / img.naturalHeight;
        } else {
            // available space may be too tall: make shorter
            canvasHeight = (canvasWidth * img.naturalHeight) / img.naturalWidth;
        }
        canvas.width = canvasWidth;
        canvas.height = canvasHeight;
        // Make the canvas fill the image container, like the img.
        // This allows object-fit and object-position to put it where we want.
        canvas.style.position = "absolute";
        canvas.style.left = "0";
        canvas.style.top = "0";
        canvas.style.height = "100%";
        canvas.style.width = "100%";
        // But then, make it shrink enough to keep its aspect ratio
        canvas.style.objectFit = "contain";
        // And position it within the container the same as the img.
        img.style.objectPosition = img.ownerDocument.defaultView // the window that the img is in (not ours!)
            .getComputedStyle(img)
            .getPropertyValue("object-position");
        canvas.classList.add("ui-cbOverlay"); // used to remove them
        var context = canvas.getContext("2d");
        context.drawImage(img, 0, 0, canvas.width, canvas.height);
        // imgData is a byte array with 4 bytes for each pixel in RGBA order
        var imgData = context.getImageData(0, 0, canvas.width, canvas.height);
        var data = imgData.data;
        var cbAdapter = deuteranopia;
        if (this.state.kindOfColorBlindness == "BlueYellow") {
            cbAdapter = tritanopia;
        } else if (this.state.kindOfColorBlindness == "Complete") {
            cbAdapter = achromatopsia;
        }
        for (var ipixel = 0; ipixel < data.length / 4; ipixel++) {
            var r = data[ipixel * 4];
            var g = data[ipixel * 4 + 1];
            var b = data[ipixel * 4 + 2];
            var colorString = this.rgbToHex(r, g, b);
            var newRgb = cbAdapter(colorString, true);
            data[ipixel * 4] = newRgb.R;
            data[ipixel * 4 + 1] = newRgb.G;
            data[ipixel * 4 + 2] = newRgb.B;
            //data[ipixel * 4 + 3] = 255; // make the new image opaque.
        }
        context.putImageData(imgData, 0, 0);

        img.parentElement.appendChild(canvas);
    }
}

// This class implements the ITool interface through our adaptor's abstract methods by calling
// the appropriate AccessibleImageControls methods.
export class AccessibleImageToolAdaptor extends ToolboxToolReactAdaptor {
    private controlsElement: AccessibleImageControls;

    public makeRootElement(): HTMLDivElement {
        return super.adaptReactElement(
            <AccessibleImageControls
                ref={renderedElement =>
                    (this.controlsElement = renderedElement)
                }
            />
        );
    }

    public id(): string {
        return "accessibleImage";
    }

    public showTool() {
        this.controlsElement.updateSimulations();
    }

    public newPageReady() {
        this.controlsElement.updateSimulations();
    }

    public detachFromPage() {
        AccessibleImageControls.removeAcessibleImageMarkup();
    }

    public isExperimental(): boolean {
        return true;
    }
}
