function xgateWhatsAppCredentialHelper(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        // 字段控件可能已从表单移除，getAttribute 会返回 null，做安全读取避免整段脚本中断
        var accountIdAttr = formContext.getAttribute("xgate_accountid");
        var accountId = accountIdAttr ? accountIdAttr.getValue() : null;

        // 核心锚点：寻找页面上的这段说明文字
        var controlElement = document.querySelector('[aria-label="To create AccountId and AuthToken"]');
        var hasAccountId = !!accountId;
        window._xgateWaHasAccountId = hasAccountId;

        // 无论是否有 AccountId，都显示 API Section 和下方的跳转按钮容器
        _xgateWaShowApiSection(controlElement);
        _xgateWaSetHeaderElement(controlElement, hasAccountId);
        _xgateWaHideHeaderSvg(controlElement);

        // 关键步骤：插入按钮容器
        _xgateWaInsertContainer(controlElement);

        _xgateWaWatchContainer();
        _xgateWaRegisterCleanup();
    } catch (error) {
        console.error("xgateWhatsAppCredentialHelper error:", error);
    }
}

// 字段可能不存在(被其他 customization 删了),做个安全包装避免抛错
function _xgateWaSafeSetVisible(formContext, fieldName, visible) {
    var control = formContext.getControl(fieldName);
    if (control) control.setVisible(visible);
}

// 根据是否填了凭证，动态更改上方文字提示
function _xgateWaSetHeaderElement(controlElement, hasAccountId) {
    if (!controlElement) return;
    var headerElement = controlElement.querySelector("h4");
    if (!headerElement) return;

    // 动态显示标题
    headerElement.textContent = hasAccountId
        ? "Manage Xgate API Credentials"
        : "Connect to Xgate to create API Credentials";
    headerElement.style.whiteSpace = "nowrap";
    headerElement.style.fontSize = "14px";
    headerElement.style.margin = "0";
    headerElement.style.padding = "0";
}

function _xgateWaHideHeaderSvg(controlElement) {
    if (!controlElement) return;
    var firstChild = controlElement.firstElementChild;
    if (!firstChild) return;
    var svgElement = firstChild.querySelector("svg");
    if (!svgElement) return;
    svgElement.style.display = "none";
}

function _xgateWaHideApiSection(controlElement) {
    if (!controlElement) return;
    controlElement.style.display = "none";
}

function _xgateWaShowApiSection(controlElement) {
    if (!controlElement) return;
    controlElement.style.display = "";
}

// ========================================================
// 核心函数：负责生成和插入卡片 UI
// ========================================================
function _xgateWaInsertContainer(controlElement) {
    if (document.getElementById("xgate-wa-trial-container")) return;
    if (!controlElement) {
        console.warn("xgateWhatsAppCredentialHelper: 未找到锚点元素 'To create AccountId and AuthToken'");
        return;
    }

    var containerDiv = document.createElement("div");
    containerDiv.id = "xgate-wa-trial-container";
    containerDiv.style.marginBottom = "10px";

    // 注入 HTML
    containerDiv.innerHTML = `
        <div class="xgate-wa-flex-container">
            <div class="xgate-wa-card">
                <h4>Create Trial Account</h4>
                <p>Create a new Xgate trial account to generate your API credentials.</p>
                <button type="button" class="xgate-wa-btn" id="xgateWaStartTrialBtn">
                    <i class="fa fa-external-link"></i> Start New Trial
                </button>
            </div>
            <div class="xgate-wa-card">
                <h4>Use Existing Account</h4>
                <p>Connect your existing Xgate account to retrieve or generate API credentials.</p>
                <button type="button" class="xgate-wa-btn" id="xgateWaConnectExistingBtn">
                    <i class="fa fa-external-link"></i> Connect Existing
                </button>
            </div>
        </div>`;

    // 插入到 D365 页面中
    controlElement.parentNode.insertBefore(containerDiv, controlElement.nextSibling);

    // 注入 CSS
    if (!document.getElementById("xgate-wa-custom-style")) {
        var style = document.createElement("style");
        style.id = "xgate-wa-custom-style";
        style.innerHTML = `
            .xgate-wa-flex-container { width: 50%; display: flex; gap: 20px; justify-content: flex-start; }
            .xgate-wa-card { background: #fff; border: 1px solid #d1d5db; border-radius: 5px; padding: 24px; min-width: 300px; flex: 1; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); }
            .xgate-wa-card h4 { font-size: 18px; font-weight: 600; margin: 0 0 12px 0; color: #111827; }
            .xgate-wa-card p { font-size: 14px; color: #6b7280; margin: 0 0 20px 0; line-height: 1.5; }
            .xgate-wa-btn { background: #2563eb; color: #fff; border: none; border-radius: 6px; padding: 8px 16px; font-size: 14px; font-weight: 500; cursor: pointer; align-items: center; gap: 6px; transition: background-color 0.2s; }
            .xgate-wa-btn:hover { background: #1d4ed8; }
            .xgate-wa-btn i { font-size: 12px; }
        `;
        document.head.appendChild(style);
    }

    // 绑定点击事件
    var startTrialBtn = document.getElementById("xgateWaStartTrialBtn");
    var connectExistingBtn = document.getElementById("xgateWaConnectExistingBtn");

    if (startTrialBtn && connectExistingBtn) {
        var openAuthWindow = function() {
            var orgUrl = "";
            var orgId = "";
            if (typeof Xrm !== "undefined" && Xrm.Utility && Xrm.Utility.getGlobalContext) {
                var context = Xrm.Utility.getGlobalContext();
                orgUrl = context.getClientUrl();
                orgId = context.organizationSettings.organizationId;
            }

            // 拼接目标 URL，携带环境信息
            var targetUrl = "https://dms4-uat.xgatecorp.com/dms4/whatsapp/login?orgUrl=" + encodeURIComponent(orgUrl) + "&orgId=" + encodeURIComponent(orgId);

            // 简单的在新标签页中打开链接
            window.open(targetUrl, "_blank");
        };

        startTrialBtn.onclick = openAuthWindow;
        connectExistingBtn.onclick = openAuthWindow;
    }
}

function _xgateWaWatchContainer() {
    if (window._xgateWaContainerObserver) return;
    window._xgateWaContainerObserver = new MutationObserver(function () {
        if (window._xgateWaMutationTickScheduled) return;
        window._xgateWaMutationTickScheduled = true;
        window.setTimeout(function () {
            window._xgateWaMutationTickScheduled = false;
            _xgateWaApplyUiState();
        }, 50);
    });
    window._xgateWaContainerObserver.observe(document.body, { childList: true, subtree: true });
}

function _xgateWaApplyUiState() {
    var controlElement = document.querySelector('[aria-label="To create AccountId and AuthToken"]');
    var hasAccountId = !!window._xgateWaHasAccountId;

    if (controlElement) {
        // 始终保持显示
        if (controlElement.style.display === "none") {
            _xgateWaShowApiSection(controlElement);
        }

        _xgateWaSetHeaderElement(controlElement, hasAccountId);
        _xgateWaHideHeaderSvg(controlElement);

        // 始终确保容器存在
        if (!document.getElementById("xgate-wa-trial-container")) {
            _xgateWaInsertContainer(controlElement);
        }
    }
}

function _xgateWaDisconnectObserver() {
    if (window._xgateWaContainerObserver) {
        window._xgateWaContainerObserver.disconnect();
        window._xgateWaContainerObserver = null;
    }
}

function _xgateWaRegisterCleanup() {
    if (window._xgateWaCleanupRegistered) return;
    window._xgateWaCleanupRegistered = true;
    window.addEventListener("beforeunload", _xgateWaDisconnectObserver);
}

function _xgateWaRemoveContainer() {
    var existing = document.getElementById("xgate-wa-trial-container");
    if (existing) existing.parentNode.removeChild(existing);
}
