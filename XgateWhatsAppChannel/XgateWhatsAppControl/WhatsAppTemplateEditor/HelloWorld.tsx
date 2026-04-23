import * as React from 'react';

export interface IHelloWorldProps {
  name?: string;
  // 【新增】这是一个回调函数，当用户输入内容时，React 会调用它把 JSON 传出去
  onDataChanged?: (jsonPayload: string) => void; 
}

// 定义组件的“状态 (State)”包含了哪些数据
interface IHelloWorldState {
  templateIdInput: string;
  isLoading: boolean;
  variables: string[];           // 存放从接口拉取回来的变量名列表，例如 ["shopName", "salesName"]
  userInputs: Record<string, string>; // 存放用户在动态输入框里填写的具体值
}

export class HelloWorld extends React.Component<IHelloWorldProps, IHelloWorldState> {
  constructor(props: IHelloWorldProps) {
    super(props);
    // 初始化状态
    this.state = {
      templateIdInput: '',
      isLoading: false,
      variables: [],
      userInputs: {}
    };
  }

  private fetchTemplateDetails = () => {
    const { templateIdInput } = this.state;
    if (!templateIdInput) return;

    this.setState({ isLoading: true, variables: [], userInputs: {} });

    // 1. 构造 Custom API 的请求体
    const request = {
      TemplateId: templateIdInput,
      getMetadata: function () {
        return {
          boundParameter: null,
          parameterTypes: {
            "TemplateId": {
              typeName: "Edm.String",
              structuralProperty: 1
            }
          },
          operationType: 0,
          operationName: "xgate_GetWhatsAppTemplate"
        };
      }
    };

    // 2. 【修复 ESLint 报错的核心】为全局 window 定义精确的 Xrm 接口类型，彻底消灭 any
    const globalWindow = window as unknown as {
      Xrm?: {
        WebApi: {
          online: {
            execute: (req: unknown) => Promise<Response>;
          };
        };
      };
    };

    const Xrm = globalWindow.Xrm;

    if (!Xrm?.WebApi) {
      alert("请将控件部署到 D365 环境中测试网络请求。本地测试台不支持 Xrm.WebApi。");
      this.setState({ isLoading: false });
      return;
    }

    // 3. 使用标准的 Response 和 Error 类型，替换掉之前的 any
    Xrm.WebApi.online.execute(request)
      .then((response: Response) => {
        if (response.ok) {
          // 强制声明返回值的类型，解决 unsafe-assignment 报错
          return response.json() as Promise<Record<string, unknown>>;
        } else {
          throw new Error(response.statusText);
        }
      })
      .then((responseBody: Record<string, unknown>) => {
        // 安全地提取字符串
        const templateData = responseBody.TemplateDataJson;
        const rawJsonString = typeof templateData === "string" ? templateData : "{}";
        console.log("C# 返回的原始数据:", rawJsonString);

        try {
          // 安全地解析 JSON
          const parsedData = JSON.parse(rawJsonString) as Record<string, unknown>;
          
          if (parsedData.code === 0 || !parsedData.error) {
            // 【你需要根据真实的 JSON 结构调整这里】
            // 假设真实结构是：{ data: { variables: ["shopName", "salesName"] } }
            const dataObj = parsedData.data as { variables?: string[] } | undefined;
            const fetchedVariables = dataObj?.variables ?? [];
            
            this.setState({
              isLoading: false,
              variables: fetchedVariables
            });
          } else {
            const errorMessage = typeof parsedData.message === "string" 
              ? parsedData.message 
              : "未知错误";
              
            alert(`后端接口报错: ${errorMessage}`);
            this.setState({ isLoading: false });
          }
        } catch (e) {
          console.error("JSON 解析失败:", e);
          this.setState({ isLoading: false });
        }
        return null;
      })
      .catch((error: Error) => {
        console.error("调用 Custom API 失败:", error);
        alert("调用后端代理接口失败，请检查网络或插件报错日志。");
        this.setState({ isLoading: false });
      });
  };

  private handleInputChange = (variableName: string, value: string) => {
    this.setState(prevState => {
      const newUserInputs = {
        ...prevState.userInputs,
        [variableName]: value
      };

      // 【关键逻辑】触发回调，把最新的 JSON 字符串交给 index.ts
      if (this.props.onDataChanged) {
        this.props.onDataChanged(JSON.stringify(newUserInputs));
      }

      return { userInputs: newUserInputs };
    });
  };

  public render(): React.ReactNode {
    const { templateIdInput, isLoading, variables, userInputs } = this.state;

    return (
      <div style={{ padding: '20px', fontFamily: 'Segoe UI, sans-serif' }}>
        <h3 style={{ color: '#25D366', borderBottom: '2px solid #25D366', paddingBottom: '10px' }}>
          💬 WhatsApp 动态模板配置
        </h3>

        {/* 1. 模板 ID 输入区域 */}
        <div style={{ marginBottom: '20px' }}>
          <label style={{ fontWeight: 'bold', display: 'block', marginBottom: '5px' }}>Template ID:</label>
          <input 
            type="text" 
            value={templateIdInput}
            onChange={(e) => this.setState({ templateIdInput: e.target.value })}
            placeholder="输入 1002015 或 1002016 测试"
            style={{ padding: '8px', width: '200px', marginRight: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
          />
          <button 
            onClick={this.fetchTemplateDetails}
            disabled={isLoading}
            style={{ padding: '8px 15px', backgroundColor: '#25D366', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
          >
            {isLoading ? '拉取中...' : '获取模板变量'}
          </button>
        </div>

        {/* 2. 动态渲染区域核心逻辑 */}
        {variables.length > 0 && (
          <div style={{ padding: '15px', backgroundColor: '#f9f9f9', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
            <h4 style={{ marginTop: 0 }}>需要配置的变量 ({variables.length} 个):</h4>
            
            {/* 遍历 variables 数组，动态生成对应的输入框 */}
            {variables.map((varName) => (
              <div key={varName} style={{ marginBottom: '10px' }}>
                <label style={{ display: 'inline-block', width: '120px', color: '#555' }}>{varName}:</label>
                <input 
                  type="text"
                  value={userInputs[varName] || ''}
                  onChange={(e) => this.handleInputChange(varName, e.target.value)}
                  placeholder={`请输入 ${varName} 的值`}
                  style={{ padding: '6px', width: '250px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
              </div>
            ))}
          </div>
        )}

        {/* 3. 实时预览我们要发给后端的 JSON */}
        <div style={{ marginTop: '20px', padding: '10px', backgroundColor: '#2d2d2d', color: '#85c46c', borderRadius: '4px', fontFamily: 'monospace' }}>
          <strong>实时组装的 JSON (将传给 C# 后端):</strong><br/>
          {JSON.stringify(userInputs, null, 2)}
        </div>

      </div>
    );
  }
}