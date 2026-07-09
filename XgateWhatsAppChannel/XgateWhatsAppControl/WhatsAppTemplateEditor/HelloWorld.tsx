import * as React from 'react';

export interface IHelloWorldProps {
  name?: string;
  // 回调函数：当用户输入/选择内容时，把最新的复合 JSON 传出去
  onDataChanged?: (jsonPayload: string) => void;
}

interface ISender {
  id: string;            // 发件号唯一 Id（发送时作为 senderId）
  senderNumber: string;  // 发件号码，用于展示
  displayName: string;   // 显示名称，用于展示
}

interface ITemplateListItem {
  id: string;            // 模板 Id（发送时作为 templateId）
  name: string;          // 模板名称，用于展示
  language: string;
  category: string;
}

// ---- 模板详情原始结构（与 Xgate template/detail 返回对齐）----
interface ITemplateHeader {
  type?: string;
  format?: string; // text | image | video | document | ...
  media?: { url?: string } | null;
  example?: { header_handle?: string[]; header_text?: string[]; variables?: string[] } | null;
}
interface ITemplateBody {
  text?: string;
  example?: { body_text?: string[][]; variables?: string[] } | null;
}
interface ITemplateButton {
  type?: string;         // url | copy_code | quick_reply | phone_number | otp | ...
  text?: string;
  url?: string;
  originalUrl?: string;  // 开启短链追踪才有，回显禁编辑
  example?: string[];
  variables?: string[];
  otp_type?: string;
}
interface ITemplateDetail {
  id?: number | string;
  name?: string;
  language?: string;
  category?: string;
  header?: ITemplateHeader | null;
  body?: ITemplateBody | null;
  footer?: { text?: string } | null;
  buttons?: { buttons?: ITemplateButton[] } | null;
  variables?: { body?: { variables?: string[] } } | null;
}

// ---- 表单映射（对齐 ManualConfigForm 的 headerMapping / bodyMapping / buttonMapping）----
type HeaderFormatKind = '' | 'text' | 'media' | 'other';
interface IHeaderInfo {
  kind: HeaderFormatKind;
  mediaFormat: string;   // image | video | document（kind=media 时有效）
  example: string;       // text header 的变量名示例
  defaultUrl: string;    // 模板自带的默认媒体 URL
}
interface IBodyVariable {
  name: string;     // 变量名（命名变量用名字，否则回退成序号 "1"/"2"）
  example: string;  // 模板给的示例值，用于占位提示
}
interface IButtonInfo {
  index: number;         // 在 buttons 数组中的原始下标
  paramsIndex: number;   // 需要传参的位置（-1 表示无需可见输入）
  variableName: string;  // 展示用变量名
}

const MEDIA_HEADER_FORMATS = new Set(['image', 'video', 'document']);
const SHORTLINK_DEFAULT = '0'; // 短链默认参数

interface IHelloWorldState {
  senders: ISender[];
  selectedSenderId: string;
  isLoadingSenders: boolean;

  templates: ITemplateListItem[];
  selectedTemplateId: string;
  isLoadingTemplates: boolean;

  detail: ITemplateDetail | null;
  headerInfo: IHeaderInfo;
  bodyVars: IBodyVariable[];
  buttonInfos: IButtonInfo[];
  buttonCount: number;                     // 需要计数的按钮总数（含默认填充位）
  buttonDefaults: Record<number, string>;  // 按 paramsIndex 预填的默认值（隐藏位）
  isLoadingVariables: boolean;

  // 用户填写的表单值
  headerText: string;                  // text header 输入
  headerMediaUrl: string;              // media header 的 URL（默认取模板媒体）
  bodyInputs: Record<string, string>;  // body 变量值（key=变量名）
  buttonInputs: Record<number, string>; // 按钮变量值（key=paramsIndex）
}

const EMPTY_HEADER: IHeaderInfo = { kind: '', mediaFormat: '', example: '', defaultUrl: '' };

export class HelloWorld extends React.Component<IHelloWorldProps, IHelloWorldState> {
  constructor(props: IHelloWorldProps) {
    super(props);
    this.state = {
      senders: [],
      selectedSenderId: '',
      isLoadingSenders: false,
      templates: [],
      selectedTemplateId: '',
      isLoadingTemplates: false,
      detail: null,
      headerInfo: EMPTY_HEADER,
      bodyVars: [],
      buttonInfos: [],
      buttonCount: 0,
      buttonDefaults: {},
      isLoadingVariables: false,
      headerText: '',
      headerMediaUrl: '',
      bodyInputs: {},
      buttonInputs: {}
    };
  }

  public componentDidMount(): void {
    this.fetchSenders();
  }

  // ============ Web API 调用封装 ============

  private getXrmWebApi(): { execute: (req: unknown) => Promise<Response> } | null {
    const globalWindow = window as unknown as {
      Xrm?: { WebApi?: { online?: { execute: (req: unknown) => Promise<Response> } } };
    };
    return globalWindow.Xrm?.WebApi?.online ?? null;
  }

  private buildRequest(operationName: string, payload: string): unknown {
    return {
      payload,
      getMetadata: () => ({
        boundParameter: null,
        parameterTypes: { payload: { typeName: 'Edm.String', structuralProperty: 1 } },
        operationType: 0,
        operationName
      })
    };
  }

  private parseInnerResponse(responseBody: Record<string, unknown>): Record<string, unknown> {
    const raw = typeof responseBody.response === 'string' ? responseBody.response : '{}';
    return JSON.parse(raw) as Record<string, unknown>;
  }

  private callCustomApi(operationName: string, payloadObj: Record<string, unknown>): Promise<Record<string, unknown>> {
    const api = this.getXrmWebApi();
    if (!api) {
      return Promise.reject(new Error('Xrm.WebApi 不可用（请在 D365 环境中运行）'));
    }
    const request = this.buildRequest(operationName, JSON.stringify(payloadObj));
    return api.execute(request)
      .then((response: Response) => {
        if (response.ok) {
          return response.json() as Promise<Record<string, unknown>>;
        }
        throw new Error(response.statusText);
      })
      .then((responseBody: Record<string, unknown>) => this.parseInnerResponse(responseBody));
  }

  // ============ 数据拉取 ============

  private fetchSenders = (): void => {
    if (!this.getXrmWebApi()) return;
    this.setState({ isLoadingSenders: true });

    this.callCustomApi('xgate_GetSendersCustomApi', {})
      .then((parsed) => {
        const senders: ISender[] = [];
        if (parsed.code === 0 && Array.isArray(parsed.data)) {
          for (const item of parsed.data as unknown[]) {
            const obj = item as Record<string, unknown>;
            const idStr = this.toIdString(obj.id);
            if (!idStr) continue;
            senders.push({
              id: idStr,
              senderNumber: this.toDisplayString(obj.senderNumber),
              displayName: typeof obj.displayName === 'string' ? obj.displayName : ''
            });
          }
        }
        this.setState({ senders, isLoadingSenders: false });
        return null;
      })
      .catch((error: Error) => {
        console.error('拉取 sender 列表失败:', error);
        this.setState({ isLoadingSenders: false });
      });
  };

  private fetchTemplates = (senderId: string): void => {
    if (!senderId || !this.getXrmWebApi()) return;
    this.setState({ isLoadingTemplates: true, templates: [] });

    this.callCustomApi('xgate_GetTemplateListCustomApi', { senderId })
      .then((parsed) => {
        const templates: ITemplateListItem[] = [];
        if (parsed.code === 0 && Array.isArray(parsed.data)) {
          for (const item of parsed.data as unknown[]) {
            const obj = item as Record<string, unknown>;
            const idStr = this.toIdString(obj.id);
            if (!idStr) continue;
            templates.push({
              id: idStr,
              name: typeof obj.name === 'string' ? obj.name : idStr,
              language: typeof obj.language === 'string' ? obj.language : '',
              category: typeof obj.category === 'string' ? obj.category : ''
            });
          }
        }
        this.setState({ templates, isLoadingTemplates: false });
        return null;
      })
      .catch((error: Error) => {
        console.error('拉取模板列表失败:', error);
        this.setState({ isLoadingTemplates: false });
      });
  };

  private fetchTemplateDetails = (templateId: string): void => {
    if (!templateId || !this.getXrmWebApi()) return;
    this.setState({ isLoadingVariables: true });

    this.callCustomApi('xgate_GetTemplateCustomApi', { templateId })
      .then((parsedData) => {
        if (parsedData.code === 0 || !parsedData.error) {
          this.applyTemplateDetail(parsedData.data as ITemplateDetail);
        } else {
          const errorMessage = typeof parsedData.message === 'string' ? parsedData.message : '未知错误';
          alert(`后端接口报错: ${errorMessage}`);
          this.setState({ isLoadingVariables: false });
        }
        return null;
      })
      .catch((error: Error) => {
        console.error('拉取模板详情失败:', error);
        alert('拉取模板详情失败，请检查网络或插件报错日志。');
        this.setState({ isLoadingVariables: false });
      });
  };

  // ============ 详情 -> 表单映射（对齐 ManualConfigForm）============

  private applyTemplateDetail(detail: ITemplateDetail): void {
    const headerInfo = this.buildHeaderInfo(detail);
    const bodyVars = this.buildBodyVariables(detail);
    const { infos, count, defaults } = this.buildButtonMapping(detail);

    this.setState({
      isLoadingVariables: false,
      detail,
      headerInfo,
      bodyVars,
      buttonInfos: infos,
      buttonCount: count,
      buttonDefaults: defaults,
      // 重置输入；media header 预填模板默认媒体
      headerText: '',
      headerMediaUrl: headerInfo.kind === 'media' ? headerInfo.defaultUrl : '',
      bodyInputs: {},
      buttonInputs: {}
    }, () => this.buildAndEmit());
  }

  private buildHeaderInfo(detail: ITemplateDetail): IHeaderInfo {
    const header = detail?.header;
    const format = header?.format;
    if (!header || !format) return EMPTY_HEADER;

    if (format === 'text') {
      const example = header.example ?? undefined;
      const headerText = example?.header_text;
      const variables = example?.variables;
      const exampleName = headerText && headerText.length === 1 ? (variables?.[0] ?? '1') : '';
      return { kind: 'text', mediaFormat: '', example: exampleName, defaultUrl: '' };
    }

    if (MEDIA_HEADER_FORMATS.has(format)) {
      const defaultUrl = header.example?.header_handle?.[0] ?? header.media?.url ?? '';
      return { kind: 'media', mediaFormat: format, example: '', defaultUrl };
    }

    return { kind: 'other', mediaFormat: '', example: '', defaultUrl: '' };
  }

  // 变量个数由 body.example.body_text[0] 决定；变量名优先用 variables，否则回退序号
  private buildBodyVariables(detail: ITemplateDetail): IBodyVariable[] {
    const example = detail?.body?.example ?? undefined;
    const bodyText = this.toStringArray(this.firstRow(example?.body_text));
    const namedVars = this.toStringArray(example?.variables);
    const fallbackNames = this.toStringArray(detail?.variables?.body?.variables);

    if (bodyText.length > 0) {
      const names = namedVars.length === bodyText.length
        ? namedVars
        : (fallbackNames.length === bodyText.length ? fallbackNames : []);
      return bodyText.map((val, k) => ({
        name: names.length === bodyText.length ? names[k] : String(k + 1),
        example: val
      }));
    }

    const names = namedVars.length > 0 ? namedVars : fallbackNames;
    return names.map((n) => ({ name: n, example: '' }));
  }

  // 对齐 ManualConfigForm 的 buttonMapping + useHandleButtonsParams
  private buildButtonMapping(detail: ITemplateDetail): { infos: IButtonInfo[]; count: number; defaults: Record<number, string> } {
    const defaults: Record<number, string> = {};
    if (detail?.category === 'AUTHENTICATION') {
      return { infos: [], count: 0, defaults };
    }
    const buttons = detail?.buttons?.buttons ?? [];
    let count = 0;
    const all: IButtonInfo[] = [];

    for (let k = 0; k < buttons.length; k++) {
      const v = buttons[k];
      let paramsIndex = -1;
      const isUrlWithExample = v.type === 'url' && !!v.example?.length && !v.otp_type;
      const isCopyCode = v.type === 'copy_code';
      if (isUrlWithExample || isCopyCode) {
        // 短链：url 带 originalUrl 且无变量时给默认值并隐藏输入
        if (v.type === 'url' && v.originalUrl && !v.variables?.length) {
          defaults[count] = SHORTLINK_DEFAULT;
        } else {
          paramsIndex = count;
        }
        count++;
      }
      all.push({
        index: k,
        paramsIndex,
        variableName: isCopyCode ? 'Copy Code' : (v.variables?.[0] ?? '1')
      });
    }

    const infos = all.filter((item) => item.paramsIndex >= 0);
    return { infos, count, defaults };
  }

  // ============ 小工具 ============

  private firstRow(raw: unknown): unknown {
    return Array.isArray(raw) ? raw[0] : undefined;
  }
  private toStringArray(raw: unknown): string[] {
    if (!Array.isArray(raw)) return [];
    return raw.map((v) => (typeof v === 'string' ? v : this.toDisplayString(v)));
  }
  private toIdString(rawId: unknown): string {
    if (typeof rawId === 'number') return String(rawId);
    if (typeof rawId === 'string') return rawId;
    return '';
  }
  private toDisplayString(raw: unknown): string {
    if (typeof raw === 'string') return raw;
    if (typeof raw === 'number') return String(raw);
    return '';
  }

  // ============ 组装并输出复合 JSON ============

  // 组装最终交给后端的复合 JSON（与写入 xgate_bodyvariables 的内容完全一致）
  private buildPayloadString(): string {
    const { selectedSenderId, selectedTemplateId, headerInfo, bodyVars, buttonCount, buttonDefaults, headerText, headerMediaUrl, bodyInputs, buttonInputs } = this.state;

    const payload: Record<string, unknown> = {
      senderId: selectedSenderId,
      templateId: selectedTemplateId
    };

    // header
    if (headerInfo.kind === 'text' && headerText) {
      payload.header = { format: 'text', text: [headerText] };
    } else if (headerInfo.kind === 'media' && headerMediaUrl) {
      payload.header = { format: headerInfo.mediaFormat, [headerInfo.mediaFormat]: { url: headerMediaUrl } };
    }

    // body：命名变量 -> 值
    const variables: Record<string, string> = {};
    for (const v of bodyVars) {
      variables[v.name] = bodyInputs[v.name] ?? '';
    }
    payload.variables = variables;

    // buttons：按位置输出（含短链默认位）
    if (buttonCount > 0) {
      const buttons: string[] = [];
      for (let i = 0; i < buttonCount; i++) {
        buttons.push(buttonInputs[i] ?? buttonDefaults[i] ?? '');
      }
      payload.buttons = buttons;
    }

    return JSON.stringify(payload);
  }

  private buildAndEmit(): void {
    if (!this.props.onDataChanged) return;
    this.props.onDataChanged(this.buildPayloadString());
  }

  // ============ 事件处理 ============

  private handleSenderChange = (senderId: string): void => {
    this.setState({
      selectedSenderId: senderId,
      selectedTemplateId: '',
      templates: [],
      detail: null,
      headerInfo: EMPTY_HEADER,
      bodyVars: [],
      buttonInfos: [],
      buttonCount: 0,
      buttonDefaults: {},
      headerText: '',
      headerMediaUrl: '',
      bodyInputs: {},
      buttonInputs: {}
    }, () => this.buildAndEmit());
    this.fetchTemplates(senderId);
  };

  private handleTemplateChange = (templateId: string): void => {
    this.setState({
      selectedTemplateId: templateId,
      detail: null,
      headerInfo: EMPTY_HEADER,
      bodyVars: [],
      buttonInfos: [],
      buttonCount: 0,
      buttonDefaults: {},
      headerText: '',
      headerMediaUrl: '',
      bodyInputs: {},
      buttonInputs: {}
    }, () => this.buildAndEmit());
    this.fetchTemplateDetails(templateId);
  };

  private handleHeaderTextChange = (value: string): void => {
    this.setState({ headerText: value }, () => this.buildAndEmit());
  };
  private handleHeaderMediaChange = (value: string): void => {
    this.setState({ headerMediaUrl: value }, () => this.buildAndEmit());
  };
  private handleBodyChange = (name: string, value: string): void => {
    this.setState(prev => ({ bodyInputs: { ...prev.bodyInputs, [name]: value } }), () => this.buildAndEmit());
  };
  private handleButtonChange = (paramsIndex: number, value: string): void => {
    this.setState(prev => ({ buttonInputs: { ...prev.buttonInputs, [paramsIndex]: value } }), () => this.buildAndEmit());
  };

  // ============ 渲染 ============

  public render(): React.ReactNode {
    const { senders, selectedSenderId, isLoadingSenders, templates, selectedTemplateId, isLoadingTemplates } = this.state;

    return (
      <div style={styles.root}>
        {/* 顶部：发件号 + 模板选择 */}
        <div style={styles.selectRow}>
          <div style={styles.field}>
            <label style={styles.label}>发件号 (Sender)</label>
            <select
              value={selectedSenderId}
              onChange={(e) => this.handleSenderChange(e.target.value)}
              disabled={isLoadingSenders}
              style={styles.select}
            >
              <option value="">{isLoadingSenders ? '发件号加载中...' : '请选择发件号'}</option>
              {senders.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.displayName ? `${s.displayName} (${s.senderNumber})` : s.senderNumber}
                </option>
              ))}
            </select>
          </div>
          <div style={styles.field}>
            <label style={styles.label}>模板 (Template)</label>
            <select
              value={selectedTemplateId}
              onChange={(e) => this.handleTemplateChange(e.target.value)}
              disabled={!selectedSenderId || isLoadingTemplates}
              style={styles.select}
            >
              <option value="">
                {!selectedSenderId ? '请先选择发件号' : (isLoadingTemplates ? '模板加载中...' : '请选择模板')}
              </option>
              {templates.map((t) => (
                <option key={t.id} value={t.id}>{t.name}（{t.id}{t.language ? ` · ${t.language}` : ''}）</option>
              ))}
            </select>
          </div>
        </div>

        {/* 主体：左表单 + 右预览（对齐 ManualConfigForm 的 horizontal 布局） */}
        {this.renderMain()}
      </div>
    );
  }

  private renderMain(): React.ReactNode {
    const { selectedTemplateId, isLoadingVariables, detail } = this.state;
    if (!selectedTemplateId) return null;
    if (isLoadingVariables) return <div style={styles.hint}>模板详情加载中...</div>;
    if (!detail) return null;

    return (
      <div style={styles.templateForm}>
        <div style={styles.formContainer}>
          {this.renderHeaderField()}
          {this.renderBodyFields()}
          {this.renderButtonFields()}
        </div>
        <div style={styles.previewContainer}>
          <label style={styles.previewLabel}>预览 (Preview)</label>
          {this.renderPreview()}
        </div>
      </div>
    );
  }

  private renderHeaderField(): React.ReactNode {
    const { headerInfo, headerText, headerMediaUrl } = this.state;
    if (headerInfo.kind === 'text') {
      return (
        <div style={styles.formItem}>
          <label style={styles.itemLabel}>页头自定义字段 (Header)</label>
          <input
            type="text"
            value={headerText}
            onChange={(e) => this.handleHeaderTextChange(e.target.value)}
            placeholder={`{{${headerInfo.example || '1'}}}`}
            style={styles.input}
          />
        </div>
      );
    }
    if (headerInfo.kind === 'media') {
      return (
        <div style={styles.formItem}>
          <label style={styles.itemLabel}>页头媒体 (Header · {headerInfo.mediaFormat})</label>
          {headerInfo.mediaFormat === 'image' && headerMediaUrl && (
            <img src={headerMediaUrl} alt="header" style={styles.mediaThumb} />
          )}
          <input
            type="text"
            value={headerMediaUrl}
            onChange={(e) => this.handleHeaderMediaChange(e.target.value)}
            placeholder="媒体文件 URL"
            style={styles.input}
          />
          <div style={styles.subHint}>默认取模板自带媒体，可替换为其它 {headerInfo.mediaFormat} 的 URL。</div>
        </div>
      );
    }
    if (headerInfo.kind === 'other') {
      return (
        <div style={styles.formItem}>
          <label style={styles.itemLabel}>页头 (Header)</label>
          <div style={styles.unsupported}>⚠ 暂不支持的页头类型</div>
        </div>
      );
    }
    return null;
  }

  private renderBodyFields(): React.ReactNode {
    const { bodyVars, bodyInputs } = this.state;
    if (bodyVars.length === 0) return null;
    return (
      <div style={styles.formItem}>
        <label style={styles.itemLabel}>正文自定义字段 (Body)</label>
        {bodyVars.map((v) => (
          <div key={v.name} style={styles.varRow}>
            <span style={styles.varTag}>{`{{${v.name}}}`}</span>
            <input
              type="text"
              value={bodyInputs[v.name] || ''}
              onChange={(e) => this.handleBodyChange(v.name, e.target.value)}
              placeholder={v.example ? `例：${v.example}` : `请输入 ${v.name}`}
              style={styles.input}
            />
          </div>
        ))}
      </div>
    );
  }

  private renderButtonFields(): React.ReactNode {
    const { buttonInfos, buttonInputs } = this.state;
    if (buttonInfos.length === 0) return null;
    return (
      <div style={styles.formItem}>
        <label style={styles.itemLabel}>按钮自定义字段 (Buttons)</label>
        {buttonInfos.map((b) => (
          <div key={b.paramsIndex} style={styles.varRow}>
            <span style={styles.varTag}>{`{{${b.variableName}}}`}</span>
            <input
              type="text"
              value={buttonInputs[b.paramsIndex] || ''}
              onChange={(e) => this.handleButtonChange(b.paramsIndex, e.target.value)}
              placeholder={`请输入 ${b.variableName}`}
              style={styles.input}
            />
          </div>
        ))}
      </div>
    );
  }

  // WhatsApp 气泡预览
  private renderPreview(): React.ReactNode {
    const { detail, headerInfo, headerMediaUrl } = this.state;
    if (!detail) return null;

    const buttons = detail.buttons?.buttons ?? [];
    const footerText = detail.footer?.text;

    return (
      <div style={styles.chatArea}>
        <div style={styles.bubble}>
          {/* header 媒体预览 */}
          {headerInfo.kind === 'media' && headerInfo.mediaFormat === 'image' && headerMediaUrl && (
            <img src={headerMediaUrl} alt="header" style={styles.bubbleImage} />
          )}
          {headerInfo.kind === 'media' && headerInfo.mediaFormat !== 'image' && (
            <div style={styles.bubbleMediaBox}>📎 {headerInfo.mediaFormat.toUpperCase()}</div>
          )}
          {/* header 文本预览 */}
          {headerInfo.kind === 'text' && (
            <div style={styles.bubbleHeaderText}>{this.previewHeaderText()}</div>
          )}
          {/* body */}
          <div style={styles.bubbleBody}>{this.previewBodyText()}</div>
          {/* footer */}
          {footerText && <div style={styles.bubbleFooter}>{footerText}</div>}
        </div>
        {/* buttons */}
        {buttons.length > 0 && (
          <div style={styles.buttonList}>
            {buttons.map((b, k) => (
              <div key={k} style={styles.previewButton}>{b.text ?? b.type}</div>
            ))}
          </div>
        )}
      </div>
    );
  }

  private previewHeaderText(): string {
    const { headerText, headerInfo } = this.state;
    if (headerText) return headerText;
    return headerInfo.example ? `{{${headerInfo.example}}}` : '';
  }

  private previewBodyText(): string {
    const { detail, bodyVars, bodyInputs } = this.state;
    let text = detail?.body?.text ?? '';
    // 把 {{1}}/{{2}} 顺序替换成用户输入（无输入则回退成示例/占位）
    for (let i = 0; i < bodyVars.length; i++) {
      const v = bodyVars[i];
      const value = bodyInputs[v.name] || v.example || `{{${v.name}}}`;
      text = text.split(`{{${i + 1}}}`).join(value);
    }
    return text;
  }
}

// ============ 样式（参考 ManualConfigForm 的布局与配色）============
const styles: Record<string, React.CSSProperties> = {
  root: { padding: '16px', fontFamily: 'Segoe UI, sans-serif', color: '#1d2129' },
  selectRow: { display: 'flex', gap: '16px', flexWrap: 'wrap', marginBottom: '16px' },
  field: { display: 'flex', flexDirection: 'column', minWidth: '260px' },
  label: { fontWeight: 600, marginBottom: '6px' },
  select: { padding: '8px', borderRadius: '4px', border: '1px solid #ccc' },
  hint: { color: '#86909c', padding: '12px 0' },
  subHint: { color: '#86909c', fontSize: '12px', marginTop: '4px' },
  // 主体两栏
  templateForm: { display: 'flex', flexDirection: 'row', gap: '24px', marginTop: '8px', alignItems: 'flex-start', flexWrap: 'wrap' },
  formContainer: { flex: '1 1 320px', maxWidth: '420px', minWidth: '300px' },
  previewContainer: { flex: '0 0 320px', minWidth: '300px' },
  previewLabel: { fontWeight: 600, display: 'block', marginBottom: '8px' },
  // 表单项
  formItem: { marginBottom: '18px' },
  itemLabel: { fontWeight: 600, display: 'block', marginBottom: '8px', color: '#4e5969' },
  input: { padding: '8px', width: '100%', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #ccc' },
  varRow: { display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '10px' },
  varTag: { flex: '0 0 auto', color: '#165DFF', background: '#E8F3FF', padding: '3px 8px', borderRadius: '3px', fontSize: '13px', whiteSpace: 'nowrap' },
  mediaThumb: { width: '100%', maxHeight: '160px', objectFit: 'cover', borderRadius: '6px', marginBottom: '8px' },
  unsupported: { color: '#F53F3F', background: '#FFECE8', padding: '8px', borderRadius: '4px' },
  // WhatsApp 预览
  chatArea: { background: '#EFEAE2', padding: '16px', borderRadius: '8px', minHeight: '160px' },
  bubble: { background: '#FFFFFF', borderRadius: '8px', padding: '8px', boxShadow: '0 1px 1px rgba(0,0,0,0.12)', maxWidth: '280px' },
  bubbleImage: { width: '100%', borderRadius: '6px', marginBottom: '6px', display: 'block' },
  bubbleMediaBox: { background: '#f2f3f5', borderRadius: '6px', padding: '24px', textAlign: 'center', color: '#86909c', marginBottom: '6px' },
  bubbleHeaderText: { fontWeight: 700, marginBottom: '4px', whiteSpace: 'pre-wrap' },
  bubbleBody: { fontSize: '14px', lineHeight: '20px', whiteSpace: 'pre-wrap', color: '#111' },
  bubbleFooter: { fontSize: '12px', color: '#86909c', marginTop: '6px', whiteSpace: 'pre-wrap' },
  buttonList: { marginTop: '6px', maxWidth: '280px' },
  previewButton: { background: '#FFFFFF', color: '#00A5F4', textAlign: 'center', padding: '8px', borderRadius: '6px', marginTop: '4px', fontSize: '14px', boxShadow: '0 1px 1px rgba(0,0,0,0.12)' }
};
