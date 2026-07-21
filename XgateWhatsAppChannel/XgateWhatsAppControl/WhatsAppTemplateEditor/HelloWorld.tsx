import * as React from 'react';
import * as COS from 'cos-js-sdk-v5';

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
  text?: string;   // text 页头的原始文本（可能是纯静态，也可能含 {{n}} 变量）
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
type HeaderFormatKind = '' | 'text' | 'statictext' | 'media' | 'other';
interface IHeaderInfo {
  kind: HeaderFormatKind;
  mediaFormat: string;   // image | video | document（kind=media 时有效）
  example: string;       // text header 的变量名示例
  defaultUrl: string;    // 模板自带的默认媒体 URL
  staticText: string;    // 纯静态文本页头（kind=statictext 时用于预览展示）
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

// 媒体上传对照表（与 QuickSend 的 uploadMap 保持一致：accept / size(MB) / 上传类型）
const MEDIA_UPLOAD_MAP: Record<string, { accept: string; size: number; type: string }> = {
  image: { accept: '.png,.jpg,.jpeg', size: 5, type: 'image' },
  video: { accept: '.mp4,.3gp,.3gpp', size: 16, type: 'video' },
  document: { accept: '.pdf', size: 100, type: 'document' }
};

// COS 临时密钥结构（对齐 /whatsapp/cos/credential 返回的扁平结构）。
// bucket / region / allowPrefix 均由接口返回（按 organizationId 做资源隔离），前端不写死。
interface ICosTmpKey {
  bucket: string;                 // 存储桶，如 dynamics365-1258635022
  region: string;                 // 地域，如 ap-hongkong
  allowPrefix?: string;           // 允许写入的前缀，如 whatsapp/{orgId}/*
  tmpSecretId: string;
  tmpSecretKey: string;
  sessionToken: string;           // sessionToken（临时密钥）
  startTime?: string | number;    // 服务端签发时间戳（秒），可选
  expiredTime: string | number;   // 过期时间戳（秒）
}

// 上传成功后组装的媒体数据，随复合 JSON 一起写入绑定字段。
// 注意：这里【故意不放完整 url】，改用 COS 组件（bucket/region/key）承载地址，
// 由 OutboundPlugin 在服务端拼回真实 URL。原因是 CIJ 对文本类渠道会无条件把消息里的
// http(s) 链接短链化（换成 usa.tx.ms 跟踪短链），导致 WhatsApp 媒体地址失效。
// 拆成不含域名的组件后，CIJ 扫不到 URL，就不会被短链化。
interface IUploadMediaData {
  bucket?: string;    // COS 存储桶
  region?: string;    // COS 地域
  key?: string;       // COS 对象 Key（不含域名，形如 whatsapp/.../x.jpg）
  url?: string;       // 兜底：无法拆成 COS 组件的媒体（如模板自带的非 COS 媒体）
  name?: string;      // 文件名
  size?: number;      // 文件大小（字节）
  mimeType?: string;  // 由文件地址推断的 MIME 类型
  mime?: string;      // 浏览器原始 File.type
}

// 保存后重新打开时，从绑定字段回传的复合 JSON（结构与 buildPayloadString 输出一致），用于回填界面
interface IRestorePayload {
  senderId?: string;
  templateId?: string;
  header?: { format?: string; text?: string[]; [key: string]: unknown };
  variables?: Record<string, string>;
  buttons?: string[];
}

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
  headerMedia: IUploadMediaData | null; // media header 上传后的完整媒体数据（默认取模板媒体）
  headerMediaPreviewUrl: string;       // 预览用地址（优先 filePreSignedUrl，回退 url）
  isUploadingMedia: boolean;           // 媒体上传中
  uploadProgress: number;              // 上传进度（0-100）
  uploadError: string;                 // 媒体上传错误提示
  bodyInputs: Record<string, string>;  // body 变量值（key=变量名）
  buttonInputs: Record<number, string>; // 按钮变量值（key=paramsIndex）
}

const EMPTY_HEADER: IHeaderInfo = { kind: '', mediaFormat: '', example: '', defaultUrl: '', staticText: '' };

// 个性化字段清单（路线二）。用户从下拉插入 token，值原样写入复合 JSON；
// 由 OutboundPlugin 在发送时按收件人（contact/lead）真实值替换。
// token 语法 [[逻辑名]]，不含 {{}}，天然绕过 CIJ 动态文本校验。
// 特殊语义键 accountname：contact→所属客户名，lead→公司名（companyname），由插件分流。
const PERSONALIZATION_FIELDS: { label: string; token: string }[] = [
  { label: 'Full name', token: '[[fullname]]' },
  { label: 'First name', token: '[[firstname]]' },
  { label: 'Last name', token: '[[lastname]]' },
  { label: 'Email', token: '[[emailaddress1]]' },
  { label: 'City', token: '[[address1_city]]' },
  { label: 'Country/Region', token: '[[address1_country]]' },
  { label: 'Salutation', token: '[[salutation]]' },
  { label: 'Account name', token: '[[accountname]]' }
];

export class HelloWorld extends React.Component<IHelloWorldProps, IHelloWorldState> {
  // 待回填的数据（保存后重新打开时从绑定字段解析而来），在 sender/template/detail 链路里逐级消费
  private restore: IRestorePayload | null = null;
  // 是否已触发过回填（绑定值可能在挂载后才由 CIJ 注入，只回填一次，避免覆盖用户编辑）
  private hasRestored = false;

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
      headerMedia: null,
      headerMediaPreviewUrl: '',
      isUploadingMedia: false,
      uploadProgress: 0,
      uploadError: '',
      bodyInputs: {},
      buttonInputs: {}
    };
  }

  // 缓存的 COS 临时密钥（有效期内复用，过期前 60s 重新拉取）
  private cosSecret: ICosTmpKey | null = null;

  // 各输入框的 DOM 引用，用于把个性化 token 插入到光标处
  private bodyInputRefs: Record<string, HTMLInputElement | null> = {};
  private headerInputRef: HTMLInputElement | null = null;
  private buttonInputRefs: Record<number, HTMLInputElement | null> = {};

  public componentDidMount(): void {
    this.fetchSenders();
    // 保存后重新打开：绑定值此刻可能已就绪；若尚未就绪，会稍后在 componentDidUpdate 里补触发
    this.maybeRestore(this.props.name);
  }

  public componentDidUpdate(prevProps: IHelloWorldProps): void {
    if (prevProps.name !== this.props.name) {
      this.maybeRestore(this.props.name);
    }
  }

  // 只在首次拿到非空绑定值时触发一次回填，避免覆盖用户后续的编辑
  private maybeRestore(raw?: string): void {
    if (this.hasRestored) return;
    const parsed = this.parseRestore(raw);
    if (!parsed) return;
    this.hasRestored = true;
    this.restore = parsed;
    // 若 sender 列表已就绪则立即回填；否则由 fetchSenders 完成后的回调消费 this.restore
    if (this.state.senders.length > 0) {
      this.tryRestoreSender();
    }
  }

  // 把绑定字段回传的字符串解析成回填结构；非法/为空/新建场景返回 null
  private parseRestore(raw?: string): IRestorePayload | null {
    const trimmed = (raw ?? '').trim();
    if (!trimmed.startsWith('{')) return null;
    try {
      const obj = JSON.parse(trimmed) as IRestorePayload;
      if (obj && (obj.senderId || obj.templateId)) return obj;
      return null;
    } catch {
      return null;
    }
  }

  // sender 列表就绪后，若有待回填的 senderId 则自动选中并继续拉模板
  private tryRestoreSender(): void {
    const r = this.restore;
    if (!r?.senderId) return;
    if (!this.state.senders.some(s => s.id === r.senderId)) {
      this.restore = null;
      return;
    }
    this.setState({ selectedSenderId: r.senderId });
    this.fetchTemplates(r.senderId);
  }

  // 模板列表就绪后，若有待回填的 templateId 则自动选中并继续拉详情
  private tryRestoreTemplate(): void {
    const r = this.restore;
    if (!r?.templateId) return;
    if (!this.state.templates.some(t => t.id === r.templateId)) {
      this.restore = null;
      return;
    }
    this.setState({ selectedTemplateId: r.templateId });
    this.fetchTemplateDetails(r.templateId);
  }

  // 把媒体对象还原成可访问的完整 URL：优先用 COS 组件拼，取不到再回退兜底 url（用于预览/展示）
  private resolveMediaUrl(m: IUploadMediaData | null | undefined): string {
    if (!m) return '';
    if (m.bucket && m.region && m.key) {
      return `https://${m.bucket}.cos.${m.region}.myqcloud.com/${m.key}`;
    }
    return m.url ?? '';
  }

  // 尝试把一个 COS 完整 URL 拆成 { bucket, region, key }；非 COS 地址返回 null
  private splitCosUrl(url: string): { bucket: string; region: string; key: string } | null {
    const m = /^https:\/\/([^./]+)\.cos\.([^./]+)\.myqcloud\.com\/(.+)$/.exec(url ?? '');
    return m ? { bucket: m[1], region: m[2], key: m[3] } : null;
  }

  // 由一个完整 URL 构造媒体对象：COS 地址拆成组件，非 COS 地址落到兜底 url
  private buildMediaFromUrl(url: string): IUploadMediaData {
    const cos = this.splitCosUrl(url);
    return cos ? { ...cos } : { url };
  }

  private extractRestoreMedia(r: IRestorePayload | null, format: string): IUploadMediaData | undefined {
    if (!r?.header) return undefined;
    const media = (r.header as Record<string, unknown>)[format] as IUploadMediaData | undefined;
    if (media && this.resolveMediaUrl(media)) return media;
    return undefined;
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
      return Promise.reject(new Error('Xrm.WebApi is unavailable (please run inside a D365 environment)'));
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
        this.setState({ senders, isLoadingSenders: false }, () => this.tryRestoreSender());
        return null;
      })
      .catch((error: Error) => {
        console.error('Failed to fetch sender list:', error);
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
        this.setState({ templates, isLoadingTemplates: false }, () => this.tryRestoreTemplate());
        return null;
      })
      .catch((error: Error) => {
        console.error('Failed to fetch template list:', error);
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
          const errorMessage = typeof parsedData.message === 'string' ? parsedData.message : 'Unknown error';
          alert(`Backend API error: ${errorMessage}`);
          this.setState({ isLoadingVariables: false });
        }
        return null;
      })
      .catch((error: Error) => {
        console.error('Failed to fetch template details:', error);
        alert('Failed to fetch template details. Please check the network or plugin error logs.');
        this.setState({ isLoadingVariables: false });
      });
  };

  // ============ 详情 -> 表单映射（对齐 ManualConfigForm）============

  private applyTemplateDetail(detail: ITemplateDetail): void {
    const headerInfo = this.buildHeaderInfo(detail);
    const bodyVars = this.buildBodyVariables(detail);
    const { infos, count, defaults } = this.buildButtonMapping(detail);

    // 回填：若为"保存后重新打开"（this.restore 有值），用保存过的值覆盖各输入，否则按新建流程重置
    const r = this.restore;
    const restoredHeaderText = headerInfo.kind === 'text' ? (r?.header?.text?.[0] ?? '') : '';
    // media header：优先回填保存过的完整媒体数据，否则回退到模板自带的默认媒体
    let restoredMedia: IUploadMediaData | null = null;
    if (headerInfo.kind === 'media') {
      restoredMedia = this.extractRestoreMedia(r, headerInfo.mediaFormat)
        ?? (headerInfo.defaultUrl ? this.buildMediaFromUrl(headerInfo.defaultUrl) : null);
    }
    const restoredMediaPreviewUrl = this.resolveMediaUrl(restoredMedia);
    const restoredBodyInputs: Record<string, string> = r?.variables ? { ...r.variables } : {};
    const restoredButtonInputs: Record<number, string> = {};
    if (Array.isArray(r?.buttons)) {
      r.buttons.forEach((val, i) => { restoredButtonInputs[i] = val; });
    }

    this.setState({
      isLoadingVariables: false,
      detail,
      headerInfo,
      bodyVars,
      buttonInfos: infos,
      buttonCount: count,
      buttonDefaults: defaults,
      headerText: restoredHeaderText,
      headerMedia: restoredMedia,
      headerMediaPreviewUrl: restoredMediaPreviewUrl,
      isUploadingMedia: false,
      uploadProgress: 0,
      uploadError: '',
      bodyInputs: restoredBodyInputs,
      buttonInputs: restoredButtonInputs
    }, () => this.buildAndEmit());

    // 回填只消费一次
    this.restore = null;
  }

  private buildHeaderInfo(detail: ITemplateDetail): IHeaderInfo {
    const header = detail?.header;
    const format = header?.format;
    if (!header || !format) return EMPTY_HEADER;

    if (format === 'text') {
      // 对齐 ManualConfigForm.headerMapping：只有当 example.header_text 恰好有 1 个变量时，
      // text 页头才需要用户输入；否则（如 hello_world 的纯静态页头）不渲染输入框，仅在预览里展示。
      const example = header.example ?? undefined;
      const headerText = example?.header_text;
      const variables = example?.variables;
      if (headerText && headerText.length === 1) {
        const exampleName = variables?.[0] ?? '1';
        return { kind: 'text', mediaFormat: '', example: exampleName, defaultUrl: '', staticText: header.text ?? '' };
      }
      return { kind: 'statictext', mediaFormat: '', example: '', defaultUrl: '', staticText: header.text ?? '' };
    }

    if (MEDIA_HEADER_FORMATS.has(format)) {
      const defaultUrl = header.example?.header_handle?.[0] ?? header.media?.url ?? '';
      return { kind: 'media', mediaFormat: format, example: '', defaultUrl, staticText: '' };
    }

    return { kind: 'other', mediaFormat: '', example: '', defaultUrl: '', staticText: '' };
  }

  // 对齐 ManualConfigForm.bodyMapping：变量个数完全由 body.example.body_text[0] 决定；
  // 变量名仅当 variables 数量与其一致时采用 variables，否则回退序号；无 body_text[0] 则不渲染正文字段。
  private buildBodyVariables(detail: ITemplateDetail): IBodyVariable[] {
    const example = detail?.body?.example ?? undefined;
    const bodyText = this.toStringArray(this.firstRow(example?.body_text));
    if (bodyText.length === 0) return [];

    const namedVars = this.toStringArray(example?.variables);
    const useNames = namedVars.length === bodyText.length;
    return bodyText.map((val, k) => ({
      name: useNames ? namedVars[k] : String(k + 1),
      example: val
    }));
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
    const { selectedSenderId, selectedTemplateId, headerInfo, bodyVars, buttonCount, buttonDefaults, headerText, headerMedia, bodyInputs, buttonInputs } = this.state;

    const payload: Record<string, unknown> = {
      senderId: selectedSenderId,
      templateId: selectedTemplateId
    };

    // header
    if (headerInfo.kind === 'text' && headerText) {
      payload.header = { format: 'text', text: [headerText] };
    } else if (headerInfo.kind === 'media' && this.resolveMediaUrl(headerMedia)) {
      // header.[format] = { bucket, region, key, name, size, mimeType, mime }（COS 组件形式，
      // 不含完整 URL，避免被 CIJ 短链化；OutboundPlugin 会在服务端拼回真实 URL）
      payload.header = { format: headerInfo.mediaFormat, [headerInfo.mediaFormat]: { ...headerMedia } };
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
    this.restore = null; // 用户手动改选，放弃任何待回填
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
      headerMedia: null,
      headerMediaPreviewUrl: '',
      isUploadingMedia: false,
      uploadProgress: 0,
      uploadError: '',
      bodyInputs: {},
      buttonInputs: {}
    }, () => this.buildAndEmit());
    this.fetchTemplates(senderId);
  };

  private handleTemplateChange = (templateId: string): void => {
    this.restore = null; // 用户手动改选，放弃任何待回填
    this.setState({
      selectedTemplateId: templateId,
      detail: null,
      headerInfo: EMPTY_HEADER,
      bodyVars: [],
      buttonInfos: [],
      buttonCount: 0,
      buttonDefaults: {},
      headerText: '',
      headerMedia: null,
      headerMediaPreviewUrl: '',
      isUploadingMedia: false,
      uploadProgress: 0,
      uploadError: '',
      bodyInputs: {},
      buttonInputs: {}
    }, () => this.buildAndEmit());
    this.fetchTemplateDetails(templateId);
  };

  private handleHeaderTextChange = (value: string): void => {
    this.setState({ headerText: value }, () => this.buildAndEmit());
  };

  // 选择文件后：本地校验 -> 拿 COS 临时密钥 -> 前端直传 COS -> 组装媒体数据（对齐 QuickSend）
  private handleMediaFileSelected = (file: File | null): void => {
    if (!file) return;
    const { headerInfo } = this.state;
    const cfg = MEDIA_UPLOAD_MAP[headerInfo.mediaFormat];
    if (!cfg) return;

    // 大小校验
    if (file.size > cfg.size * 1024 * 1024) {
      this.setState({ uploadError: `File exceeds the size limit (max ${cfg.size}MB)` });
      return;
    }
    // 扩展名校验
    const ext = (file.name.split('.').pop() ?? '').toLowerCase();
    const accepts = cfg.accept.split(',').map(s => s.trim().toLowerCase());
    if (!accepts.includes('.' + ext)) {
      this.setState({ uploadError: `Unsupported file format (only ${cfg.accept} allowed)` });
      return;
    }

    const type = cfg.type;
    // 文件名：{时间戳}_{随机}.{ext}；完整对象 Key 与地址在拿到密钥后按返回的 Bucket/Region/Prefix 组装
    const filename = `${Math.floor(Date.now() / 1000)}_${Math.round(Math.random() * 999999)}.${ext}`;

    this.setState({ isUploadingMedia: true, uploadProgress: 0, uploadError: '' });

    let fileUrl = '';
    let cosBucket = '';
    let cosRegion = '';
    let cosKey = '';
    this.getCosCredentials()
      .then((secret) => {
        // Key：{allowPrefix去掉尾部 /*}/{type}/{filename}，最终地址按返回的 bucket/region 拼
        const base = (secret.allowPrefix ?? '').replace(/\*+$/, '').replace(/^\/+|\/+$/g, '');
        const objectKey = [base, type, filename].filter((p) => p).join('/');
        cosBucket = secret.bucket;
        cosRegion = secret.region;
        cosKey = objectKey;
        // fileUrl 仅用于本地预览；写入绑定字段的是拆开的 bucket/region/key，避免被 CIJ 短链化
        fileUrl = `https://${secret.bucket}.cos.${secret.region}.myqcloud.com/${objectKey}`;

        const cos = new COS({
          getAuthorization: (_options, callback) => {
            callback({
              TmpSecretId: secret.tmpSecretId,
              TmpSecretKey: secret.tmpSecretKey,
              SecurityToken: secret.sessionToken,
              StartTime: Number(secret.startTime) || Math.floor(Date.now() / 1000),
              ExpiredTime: Number(secret.expiredTime)
            });
          }
        });

        const params: COS.PutObjectParams = {
          Bucket: secret.bucket,
          Region: secret.region,
          Key: objectKey,
          StorageClass: 'STANDARD',
          Body: file,
          onProgress: (progressData) => {
            const percent = progressData?.percent ? Math.round(progressData.percent * 100) : 0;
            this.setState({ uploadProgress: percent });
          }
        };

        return cos.putObject(params);
      })
      .then(() => {
        // 写入绑定字段的是拆开的 COS 组件（不含完整 URL），由 OutboundPlugin 服务端拼回真实地址
        const media: IUploadMediaData = {
          bucket: cosBucket,
          region: cosRegion,
          key: cosKey,
          name: file.name,
          size: file.size,
          mimeType: this.guessMimeType(fileUrl) || file.type,
          mime: file.type
        };
        this.setState({
          headerMedia: media,
          headerMediaPreviewUrl: fileUrl,
          isUploadingMedia: false,
          uploadProgress: 100,
          uploadError: ''
        }, () => this.buildAndEmit());
        return null;
      })
      .catch((error: Error) => {
        console.error('COS upload failed:', error);
        this.setState({ isUploadingMedia: false, uploadError: error.message || 'Upload failed, please try again' });
      });
  };

  // 获取（并缓存）COS 临时密钥；过期前 60s 复用缓存，否则经 Custom API 重新拉取
  private getCosCredentials(): Promise<ICosTmpKey> {
    const now = Math.floor(Date.now() / 1000);
    if (this.cosSecret && Number(this.cosSecret.expiredTime) - 60 > now) {
      return Promise.resolve(this.cosSecret);
    }
    // Custom API 插件把凭证接口的响应体原样透传，因此 parsed 即扁平的凭证对象
    return this.callCustomApi('xgate_GetCosTmpKeyCustomApi', {}).then((parsed) => {
      const data = parsed as unknown as ICosTmpKey;
      if (!data?.bucket || !data?.region || !data?.expiredTime || !data?.tmpSecretId || !data?.tmpSecretKey || !data?.sessionToken) {
        const msg = typeof parsed.message === 'string' ? parsed.message : 'Failed to get COS temporary credentials (response is missing required fields)';
        throw new Error(msg);
      }
      this.cosSecret = data;
      return data;
    });
  }

  private handleRemoveMedia = (): void => {
    this.setState({ headerMedia: null, headerMediaPreviewUrl: '', uploadProgress: 0, uploadError: '' }, () => this.buildAndEmit());
  };

  // 由文件地址扩展名推断 MIME（对齐 QuickSend 的 mime.getType(fileUrl)）
  private guessMimeType(url: string): string {
    const clean = url.split('?')[0];
    const ext = (clean.split('.').pop() ?? '').toLowerCase();
    const map: Record<string, string> = {
      png: 'image/png', jpg: 'image/jpeg', jpeg: 'image/jpeg', webp: 'image/webp', gif: 'image/gif',
      mp4: 'video/mp4', '3gp': 'video/3gpp', '3gpp': 'video/3gpp', pdf: 'application/pdf'
    };
    return map[ext] || '';
  }
  private handleBodyChange = (name: string, value: string): void => {
    this.setState(prev => ({ bodyInputs: { ...prev.bodyInputs, [name]: value } }), () => this.buildAndEmit());
  };
  private handleButtonChange = (paramsIndex: number, value: string): void => {
    this.setState(prev => ({ buttonInputs: { ...prev.buttonInputs, [paramsIndex]: value } }), () => this.buildAndEmit());
  };

  // ============ 个性化字段插入 ============

  // 把 token 插入到输入框当前光标处（拿不到光标位置则追加到末尾），并把光标移动到插入内容之后
  private insertAtCaret(el: HTMLInputElement | null, current: string, token: string): { next: string; caret: number } {
    if (el && typeof el.selectionStart === 'number') {
      const start = el.selectionStart;
      const end = typeof el.selectionEnd === 'number' ? el.selectionEnd : start;
      const next = current.slice(0, start) + token + current.slice(end);
      return { next, caret: start + token.length };
    }
    return { next: current + token, caret: (current + token).length };
  }

  private restoreCaret(el: HTMLInputElement | null, caret: number): void {
    if (!el) return;
    try {
      el.focus();
      el.setSelectionRange(caret, caret);
    } catch {
      /* 某些环境不支持 setSelectionRange，忽略即可 */
    }
  }

  private insertBodyToken = (name: string, token: string): void => {
    const el = this.bodyInputRefs[name];
    const { next, caret } = this.insertAtCaret(el, this.state.bodyInputs[name] || '', token);
    this.setState(prev => ({ bodyInputs: { ...prev.bodyInputs, [name]: next } }), () => {
      this.buildAndEmit();
      this.restoreCaret(this.bodyInputRefs[name], caret);
    });
  };

  private insertHeaderToken = (token: string): void => {
    const el = this.headerInputRef;
    const { next, caret } = this.insertAtCaret(el, this.state.headerText, token);
    this.setState({ headerText: next }, () => {
      this.buildAndEmit();
      this.restoreCaret(this.headerInputRef, caret);
    });
  };

  private insertButtonToken = (paramsIndex: number, token: string): void => {
    const el = this.buttonInputRefs[paramsIndex];
    const { next, caret } = this.insertAtCaret(el, this.state.buttonInputs[paramsIndex] || '', token);
    this.setState(prev => ({ buttonInputs: { ...prev.buttonInputs, [paramsIndex]: next } }), () => {
      this.buildAndEmit();
      this.restoreCaret(this.buttonInputRefs[paramsIndex], caret);
    });
  };

  // 个性化字段下拉：选中即插入对应 token，随后重置回占位项
  private renderFieldPicker(onInsert: (token: string) => void): React.ReactNode {
    return (
      <select
        value=""
        title="Insert personalization field (replaced with the recipient's real value at send time)"
        style={styles.fieldPicker}
        onChange={(e) => {
          const token = e.target.value;
          e.target.value = '';
          if (token) onInsert(token);
        }}
      >
        <option value="">Personalization</option>
        {PERSONALIZATION_FIELDS.map((f) => (
          <option key={f.token} value={f.token}>{f.label}</option>
        ))}
      </select>
    );
  }

  // ============ 渲染 ============

  public render(): React.ReactNode {
    const { senders, selectedSenderId, isLoadingSenders, templates, selectedTemplateId, isLoadingTemplates } = this.state;

    return (
      <div style={styles.root}>
        {/* 顶部：发件号 + 模板选择 */}
        <div style={styles.selectRow}>
          <div style={styles.field}>
            <label style={styles.label}>Sender</label>
            <select
              value={selectedSenderId}
              onChange={(e) => this.handleSenderChange(e.target.value)}
              disabled={isLoadingSenders}
              style={styles.select}
            >
              <option value="">{isLoadingSenders ? 'Loading senders...' : 'Select a sender'}</option>
              {senders.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.displayName ? `${s.displayName} (${s.senderNumber})` : s.senderNumber}
                </option>
              ))}
            </select>
          </div>
          <div style={styles.field}>
            <label style={styles.label}>Template</label>
            <select
              value={selectedTemplateId}
              onChange={(e) => this.handleTemplateChange(e.target.value)}
              disabled={!selectedSenderId || isLoadingTemplates}
              style={styles.select}
            >
              <option value="">
                {!selectedSenderId ? 'Select a sender first' : (isLoadingTemplates ? 'Loading templates...' : 'Select a template')}
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
    if (isLoadingVariables) return <div style={styles.hint}>Loading template details...</div>;
    if (!detail) return null;

    return (
      <div style={styles.templateForm}>
        <div style={styles.formContainer}>
          {this.renderHeaderField()}
          {this.renderBodyFields()}
          {this.renderButtonFields()}
        </div>
        <div style={styles.previewContainer}>
          <label style={styles.previewLabel}>Preview</label>
          {this.renderPreview()}
        </div>
      </div>
    );
  }

  private renderHeaderField(): React.ReactNode {
    const { headerInfo, headerText } = this.state;
    if (headerInfo.kind === 'text') {
      return (
        <div style={styles.formItem}>
          <label style={styles.itemLabel}>Header variable</label>
          <div style={styles.varRow}>
            <input
              ref={(el) => { this.headerInputRef = el; }}
              type="text"
              value={headerText}
              onChange={(e) => this.handleHeaderTextChange(e.target.value)}
              placeholder={`{{${headerInfo.example || '1'}}}`}
              style={styles.input}
            />
            {this.renderFieldPicker((token) => this.insertHeaderToken(token))}
          </div>
        </div>
      );
    }
    // 纯静态文本页头：内容固定、无变量，无需用户输入，只在右侧预览里展示
    if (headerInfo.kind === 'statictext') {
      return null;
    }
    if (headerInfo.kind === 'media') {
      return this.renderMediaUploader();
    }
    if (headerInfo.kind === 'other') {
      return (
        <div style={styles.formItem}>
          <label style={styles.itemLabel}>Header</label>
          <div style={styles.unsupported}>⚠ Unsupported header type</div>
        </div>
      );
    }
    return null;
  }

  // 媒体上传区（对齐 QuickSend：上传文件 -> COS，替换/删除，展示文件名与预览）
  private renderMediaUploader(): React.ReactNode {
    const { headerInfo, headerMedia, headerMediaPreviewUrl, isUploadingMedia, uploadProgress, uploadError } = this.state;
    const cfg = MEDIA_UPLOAD_MAP[headerInfo.mediaFormat] ?? { accept: '', size: 0, type: '' };
    return (
      <div style={styles.formItem}>
        <label style={styles.itemLabel}>Header media ({headerInfo.mediaFormat})</label>

        {headerInfo.mediaFormat === 'image' && headerMediaPreviewUrl && (
          <img src={headerMediaPreviewUrl} alt="header" style={styles.mediaThumb} />
        )}

        {this.resolveMediaUrl(headerMedia) && (
          <div style={styles.fileRow}>
            <span style={styles.fileName}>{headerMedia?.name ?? this.resolveMediaUrl(headerMedia)}</span>
            <button type="button" style={styles.removeBtn} onClick={this.handleRemoveMedia} disabled={isUploadingMedia}>Remove</button>
          </div>
        )}

        <label style={{ ...styles.uploadBtn, ...(isUploadingMedia ? styles.uploadBtnDisabled : {}) }}>
          {isUploadingMedia ? `Uploading... ${uploadProgress}%` : (this.resolveMediaUrl(headerMedia) ? 'Replace file' : 'Upload file')}
          <input
            type="file"
            accept={cfg.accept}
            disabled={isUploadingMedia}
            style={{ display: 'none' }}
            onChange={(e) => {
              const file = e.target.files?.[0] ?? null;
              this.handleMediaFileSelected(file);
              e.target.value = ''; // 允许重复选择同一文件
            }}
          />
        </label>

        {isUploadingMedia && (
          <div style={styles.progressBar}>
            <div style={{ ...styles.progressInner, width: `${uploadProgress}%` }} />
          </div>
        )}

        {uploadError && <div style={styles.uploadError}>{uploadError}</div>}
        <div style={styles.subHint}>Uses the built-in template media by default; you can upload a replacement (direct upload to Tencent Cloud COS). {cfg.accept}, max {cfg.size}MB.</div>
      </div>
    );
  }

  private renderBodyFields(): React.ReactNode {
    const { bodyVars, bodyInputs } = this.state;
    if (bodyVars.length === 0) return null;
    return (
      <div style={styles.formItem}>
        <label style={styles.itemLabel}>Body variables</label>
        {bodyVars.map((v) => (
          <div key={v.name} style={styles.varRow}>
            <span style={styles.varTag}>{`{{${v.name}}}`}</span>
            <input
              ref={(el) => { this.bodyInputRefs[v.name] = el; }}
              type="text"
              value={bodyInputs[v.name] || ''}
              onChange={(e) => this.handleBodyChange(v.name, e.target.value)}
              placeholder={`Enter ${v.name}`}
              style={styles.input}
            />
            {this.renderFieldPicker((token) => this.insertBodyToken(v.name, token))}
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
        <label style={styles.itemLabel}>Button variables</label>
        {buttonInfos.map((b) => (
          <div key={b.paramsIndex} style={styles.varRow}>
            <span style={styles.varTag}>{`{{${b.variableName}}}`}</span>
            <input
              ref={(el) => { this.buttonInputRefs[b.paramsIndex] = el; }}
              type="text"
              value={buttonInputs[b.paramsIndex] || ''}
              onChange={(e) => this.handleButtonChange(b.paramsIndex, e.target.value)}
              placeholder={`Enter ${b.variableName}`}
              style={styles.input}
            />
            {this.renderFieldPicker((token) => this.insertButtonToken(b.paramsIndex, token))}
          </div>
        ))}
      </div>
    );
  }

  // WhatsApp 气泡预览
  private renderPreview(): React.ReactNode {
    const { detail, headerInfo, headerMediaPreviewUrl } = this.state;
    if (!detail) return null;

    const buttons = detail.buttons?.buttons ?? [];
    const footerText = detail.footer?.text;

    return (
      <div style={styles.chatArea}>
        <div style={styles.bubble}>
          {/* header 媒体预览 */}
          {headerInfo.kind === 'media' && headerInfo.mediaFormat === 'image' && headerMediaPreviewUrl && (
            <img src={headerMediaPreviewUrl} alt="header" style={styles.bubbleImage} />
          )}
          {headerInfo.kind === 'media' && headerInfo.mediaFormat !== 'image' && (
            <div style={styles.bubbleMediaBox}>📎 {headerInfo.mediaFormat.toUpperCase()}</div>
          )}
          {/* header 文本预览（含带变量的 text 与纯静态 statictext） */}
          {(headerInfo.kind === 'text' || headerInfo.kind === 'statictext') && (
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
    // 纯静态页头：直接展示模板自带的固定文本
    if (headerInfo.kind === 'statictext') return headerInfo.staticText;
    if (headerText) return headerText;
    return headerInfo.example ? `{{${headerInfo.example}}}` : '';
  }

  private previewBodyText(): string {
    const { detail, bodyVars, bodyInputs } = this.state;
    let text = detail?.body?.text ?? '';
    // 对齐 ContentTemplate.template2HTML：example 仅用于数变量个数，不作为预览显示内容。
    // 未输入的变量在预览里显示占位 {{name}}，只有用户真正填了值才替换成实际内容。
    for (let i = 0; i < bodyVars.length; i++) {
      const v = bodyVars[i];
      const input = bodyInputs[v.name];
      const value = input && input.length > 0 ? input : `{{${v.name}}}`;
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
  fieldPicker: { flex: '0 0 auto', padding: '6px', borderRadius: '4px', border: '1px solid #ccc', background: '#fff', color: '#4e5969', fontSize: '13px', cursor: 'pointer', maxWidth: '110px' },
  mediaThumb: { width: '100%', maxHeight: '160px', objectFit: 'cover', borderRadius: '6px', marginBottom: '8px' },
  unsupported: { color: '#F53F3F', background: '#FFECE8', padding: '8px', borderRadius: '4px' },
  // 媒体上传
  uploadBtn: { display: 'inline-block', padding: '8px 16px', background: '#165DFF', color: '#fff', borderRadius: '4px', cursor: 'pointer', fontSize: '13px', textAlign: 'center' },
  uploadBtnDisabled: { background: '#94BFFF', cursor: 'not-allowed' },
  fileRow: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', padding: '6px 8px', background: '#F2F3F5', borderRadius: '4px', marginBottom: '8px' },
  fileName: { fontSize: '13px', color: '#1d2129', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' },
  removeBtn: { flex: '0 0 auto', border: 'none', background: 'transparent', color: '#F53F3F', cursor: 'pointer', fontSize: '13px' },
  progressBar: { height: '6px', background: '#E5E6EB', borderRadius: '3px', marginTop: '8px', overflow: 'hidden' },
  progressInner: { height: '100%', background: '#165DFF', borderRadius: '3px', transition: 'width 0.2s' },
  uploadError: { color: '#F53F3F', fontSize: '12px', marginTop: '6px' },
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
