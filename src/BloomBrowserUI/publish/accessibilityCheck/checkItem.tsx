import * as React from "react";
import { IUILanguageAwareProps } from "../../react_components/l10n";
import axios from "axios";

interface IProps extends IUILanguageAwareProps {
    apiCheckName: string;
    label: string;
    // The parent can give us this function which we use to subscribe to refresh events
    // See notes in accessibiltiyChecklist for a thorough discussion.
    subscribeToRefresh?: (queryData: () => void) => void;
}

// Each "CheckItem" conveys the status and results of a single automated accessibility test.
// this should match the struct Problem in AccessibilityCheckers.cs
interface IProblem {
    message: string;
    problemText: string;
}
interface CheckResult {
    // this is passed, failed, unknown, or pending. Only the stylesheet cares, this code doesn't.
    resultClass: string;
    // for now, simple strings. Someday may be links to problem items.
    problems: IProblem[];
}

interface IState {
    checkResult: CheckResult;
}
export class CheckItem extends React.Component<IProps, IState> {
    constructor(props) {
        super(props);
        this.state = {
            checkResult: { resultClass: "pending", problems: [] }
        };
    }

    public componentDidMount() {
        this.queryData();

        if (this.props.subscribeToRefresh) {
            this.props.subscribeToRefresh(() => this.queryData());
        }
    }

    private queryData() {
        axios
            .get(`/bloom/api/accessibilityCheck/${this.props.apiCheckName}`)
            .then(result => {
                this.setState({ checkResult: result.data });
            })
            .catch(error => {
                this.setState({
                    checkResult: {
                        resultClass: "unknown",
                        problems: [error.response.statusText]
                    }
                });
            });
    }

    public render() {
        return (
            <li className={`checkItem ${this.state.checkResult.resultClass}`}>
                {
                    this.props.label // TODO Make localizable, just based on our props.apiCheckName
                }
                <ul>
                    {// problem descriptions are already localized by the backend
                    this.state.checkResult.problems.map((problem, index) => (
                        //react requires unique keys on each
                        <li key={"p" + index}>
                            {problem.message}
                            {problem.problemText ? (
                                <blockquote>{problem.problemText}</blockquote>
                            ) : (
                                ""
                            )}
                        </li>
                    ))}
                </ul>
            </li>
        );
    }
}
