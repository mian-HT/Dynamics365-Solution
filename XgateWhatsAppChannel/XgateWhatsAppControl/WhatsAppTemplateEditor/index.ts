import { IInputs, IOutputs } from "./generated/ManifestTypes";
import * as React from "react";
import { HelloWorld, IHelloWorldProps } from "./HelloWorld";

export class WhatsAppTemplateEditor implements ComponentFramework.ReactControl<IInputs, IOutputs> {

    private notifyOutputChanged: () => void;
    private currentJsonPayload: string | null;

    /**
     * 1. 控件初始化
     */
    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary
    ): void {
        this.notifyOutputChanged = notifyOutputChanged;
        // 记录初始值
        this.currentJsonPayload = context.parameters.sampleProperty.raw ?? "";
    }

    /**
     * 2. 渲染视图：当数据变化时触发
     */
    public updateView(context: ComponentFramework.Context<IInputs>): React.ReactElement {
        const props: IHelloWorldProps = {
            name: context.parameters.sampleProperty.raw ?? "",
            onDataChanged: this.handleReactDataChange
        };

        return React.createElement(HelloWorld, props);
    }

    /**
     * 3. 我们自定义的回调：接收 React 传来的最新 JSON
     */
    private handleReactDataChange = (newJsonString: string) => {
        this.currentJsonPayload = newJsonString;
        this.notifyOutputChanged(); // 敲响 D365 的警钟
    };

    /**
     * 4. D365 听到警钟后来拿数据
     */
    public getOutputs(): IOutputs {
        return {
            sampleProperty: this.currentJsonPayload ?? ""
        };
    }

    /**
     * 5. 资源清理
     */
    public destroy(): void {
        // 在 ReactControl 模式下，DOM 的清理和组件卸载完全由 D365 引擎自动接管，不需要我们写代码
    }
}
