function xgateCredentialHelper(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var accountId = formContext.getAttribute("xgate_accountid").getValue();

        // 核心锚点：寻找页面上的这段说明文字
        var controlElement = document.querySelector('[aria-label="To create AccountId and AccountSecret"]');
        var hasAccountId = !!accountId;
        window._xgateHasAccountId = hasAccountId;

        // 无论是否有 AccountId，都显示 API Section 和下方的跳转按钮容器
        _xgateShowApiSection(controlElement);
        _xgateSetHeaderElement(controlElement, hasAccountId);
        _xgateHideHeaderSvg(controlElement);
        
        // 关键步骤：插入按钮容器
        _xgateInsertContainer(controlElement);

        _xgateWatchContainer();
        _xgateRegisterCleanup();
    } catch (error) {
        console.error("xgateCredentialHelper error:", error);
    }
}

// 字段可能不存在(被其他 customization 删了),做个安全包装避免抛错
function _xgateSafeSetVisible(formContext, fieldName, visible) {
    var control = formContext.getControl(fieldName);
    if (control) control.setVisible(visible);
}

// 根据是否填了凭证，动态更改上方文字提示
function _xgateSetHeaderElement(controlElement, hasAccountId) {
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

function _xgateHideHeaderSvg(controlElement) {
    if (!controlElement) return;
    var firstChild = controlElement.firstElementChild;
    if (!firstChild) return;
    var svgElement = firstChild.querySelector("svg");
    if (!svgElement) return;
    svgElement.style.display = "none";
}

function _xgateHideApiSection(controlElement) {
    if (!controlElement) return;
    controlElement.style.display = "none";
}

function _xgateShowApiSection(controlElement) {
    if (!controlElement) return;
    controlElement.style.display = "";
}

// ========================================================
// 🚨 之前丢失的核心函数：负责生成和插入卡片 UI 🚨
// ========================================================
function _xgateInsertContainer(controlElement) {
    if (document.getElementById("xgate-trial-container")) return;
    if (!controlElement) {
        console.warn("xgateCredentialHelper: 未找到锚点元素 'To create AccountId and AccountSecret'");
        return;
    }

    var containerDiv = document.createElement("div");
    containerDiv.id = "xgate-trial-container";
    containerDiv.style.marginBottom = "10px";
    
    // 注入 HTML
    containerDiv.innerHTML = `
        <div class="xgate-flex-container">
            <div class="xgate-card">
                <h4>Create Trial Account</h4>
                <p>Create a new Xgate trial account to generate your API credentials.</p>
                <button type="button" class="xgate-btn" id="xgateStartTrialBtn">
                    <i class="fa fa-external-link"></i> Start New Trial
                </button>
            </div>
            <div class="xgate-card">
                <h4>Use Existing Account</h4>
                <p>Connect your existing Xgate account to retrieve or generate API credentials.</p>
                <button type="button" class="xgate-btn" id="xgateConnectExistingBtn">
                    <i class="fa fa-external-link"></i> Connect Existing
                </button>
            </div>
        </div>`;

    // 插入到 D365 页面中
    controlElement.parentNode.insertBefore(containerDiv, controlElement.nextSibling);

    // 注入 CSS
    if (!document.getElementById("xgate-custom-style")) {
        var style = document.createElement("style");
        style.id = "xgate-custom-style";
        style.innerHTML = `
            .xgate-flex-container { width: 50%; display: flex; gap: 20px; justify-content: flex-start; }
            .xgate-card { background: #fff; border: 1px solid #d1d5db; border-radius: 5px; padding: 24px; min-width: 300px; flex: 1; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1); }
            .xgate-card h4 { font-size: 18px; font-weight: 600; margin: 0 0 12px 0; color: #111827; }
            .xgate-card p { font-size: 14px; color: #6b7280; margin: 0 0 20px 0; line-height: 1.5; }
            .xgate-btn { background: #2563eb; color: #fff; border: none; border-radius: 6px; padding: 8px 16px; font-size: 14px; font-weight: 500; cursor: pointer; align-items: center; gap: 6px; transition: background-color 0.2s; }
            .xgate-btn:hover { background: #1d4ed8; }
            .xgate-btn i { font-size: 12px; }
        `;
        document.head.appendChild(style);
    }

    // 绑定点击事件
    var startTrialBtn = document.getElementById("xgateStartTrialBtn");
    var connectExistingBtn = document.getElementById("xgateConnectExistingBtn");

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
            var targetUrl = "https://dms4-uat.xgatecorp.com/dms4/smsc/login?orgUrl=" + encodeURIComponent(orgUrl) + "&orgId=" + encodeURIComponent(orgId);
            
            // 简单的在新标签页中打开链接
            window.open(targetUrl, "_blank");
        };

        startTrialBtn.onclick = openAuthWindow;
        connectExistingBtn.onclick = openAuthWindow;
    }
}

function _xgateWatchContainer() {
    if (window._xgateContainerObserver) return;
    window._xgateContainerObserver = new MutationObserver(function () {
        if (window._xgateMutationTickScheduled) return;
        window._xgateMutationTickScheduled = true;
        window.setTimeout(function () {
            window._xgateMutationTickScheduled = false;
            _xgateApplyUiState();
        }, 50);
    });
    window._xgateContainerObserver.observe(document.body, { childList: true, subtree: true });
}

function _xgateApplyUiState() {
    var controlElement = document.querySelector('[aria-label="To create AccountId and AccountSecret"]');
    var hasAccountId = !!window._xgateHasAccountId;

    if (controlElement) {
        // 始终保持显示
        if (controlElement.style.display === "none") {
            _xgateShowApiSection(controlElement);
        }
        
        _xgateSetHeaderElement(controlElement, hasAccountId);
        _xgateHideHeaderSvg(controlElement);
        
        // 始终确保容器存在
        if (!document.getElementById("xgate-trial-container")) {
            _xgateInsertContainer(controlElement);
        }
    }
}

function _xgateDisconnectObserver() {
    if (window._xgateContainerObserver) {
        window._xgateContainerObserver.disconnect();
        window._xgateContainerObserver = null;
    }
}

function _xgateRegisterCleanup() {
    if (window._xgateCleanupRegistered) return;
    window._xgateCleanupRegistered = true;
    window.addEventListener("beforeunload", _xgateDisconnectObserver);
}

function _xgateRemoveContainer() {
    var existing = document.getElementById("xgate-trial-container");
    if (existing) existing.parentNode.removeChild(existing);
}