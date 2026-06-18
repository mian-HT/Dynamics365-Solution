function xgateCredentialHelper(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        var accountId = formContext.getAttribute("xgate_accountid").getValue();

        // 删掉了藏字段的逻辑 —— AccountId / Secret 始终显示
        // 用户可以选择:
        //   1) 直接粘贴已有凭据到字段里
        //   2) 点卡片去 Xgate 申请新凭据再回来粘贴

        var controlElement = document.querySelector('[aria-label="To create AccountId and AccountSecret"]');
        var hasAccountId = !!accountId;
        window._xgateHasAccountId = hasAccountId;

        if (!hasAccountId) {
            _xgateShowApiSection(controlElement);
            _xgateSetHeaderElement(controlElement);
            _xgateHideHeaderSvg(controlElement);
            _xgateInsertContainer(controlElement);
        } else {
            _xgateHideApiSection(controlElement);
            _xgateRemoveContainer();
        }

        _xgateWatchContainer();
        _xgateRegisterCleanup();
    } catch (error) {
        console.error("xgateCredentialHelper error:", error);
    }
}

// 字段可能不存在(被其他 customization 删了),做个安全包装避免抛错
function _xgateSafeSetVisible(formContext, fieldName, visible) {
    var control = formContext.getControl(fieldName);
    if (control) {
        control.setVisible(visible);
    }
}

function _xgateSetHeaderElement(controlElement) {
    if (!controlElement) return;
    var headerElement = controlElement.querySelector("h4");
    if (!headerElement) return;

    headerElement.textContent = "Connect to Xgate to create API Credentials";
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

function _xgateInsertContainer(controlElement) {
    if (document.getElementById("xgate-trial-container")) return;
    if (!controlElement) {
        console.error("xgateCredentialHelper: anchor element not found.");
        return;
    }

    var containerDiv = document.createElement("div");
    containerDiv.id = "xgate-trial-container";
    containerDiv.style.marginBottom = "10px";
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

    controlElement.parentNode.insertBefore(containerDiv, controlElement.nextSibling);

    if (!document.getElementById("xgate-custom-style")) {
        var style = document.createElement("style");
        style.id = "xgate-custom-style";
        style.innerHTML = `
            .xgate-flex-container {
                width: 50%;
                display: flex;
                gap: 20px;
                justify-content: flex-start;
            }
            .xgate-card {
                background: #fff;
                border: 1px solid #d1d5db;
                border-radius: 5px;
                padding: 24px;
                min-width: 300px;
                flex: 1;
                box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
            }
            .xgate-card h4 {
                font-size: 18px;
                font-weight: 600;
                margin: 0 0 12px 0;
                color: #111827;
            }
            .xgate-card p {
                font-size: 14px;
                color: #6b7280;
                margin: 0 0 20px 0;
                line-height: 1.5;
            }
            .xgate-btn {
                background: #2563eb;
                color: #fff;
                border: none;
                border-radius: 6px;
                padding: 8px 16px;
                font-size: 14px;
                font-weight: 500;
                cursor: pointer;
                align-items: center;
                gap: 6px;
                transition: background-color 0.2s;
            }
            .xgate-btn:hover {
                background: #1d4ed8;
            }
            .xgate-btn i {
                font-size: 12px;
            }
        `;
        document.head.appendChild(style);
    }

    var startTrialBtn = document.getElementById("xgateStartTrialBtn");
    var connectExistingBtn = document.getElementById("xgateConnectExistingBtn");

    if (startTrialBtn && connectExistingBtn) {
        // TODO: 替换成你真实的 Xgate 注册和 console 链接
        startTrialBtn.onclick = function () {
            window.open("https://smsc.xgate.com.hk", "_blank");
        };
        connectExistingBtn.onclick = function () {
            window.open("https://smsc.xgate.com.hk", "_blank");
        };
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
        if (hasAccountId) {
            if (controlElement.style.display !== "none") {
                _xgateHideApiSection(controlElement);
            }
            if (document.getElementById("xgate-trial-container")) {
                _xgateRemoveContainer();
            }
        } else {
            if (controlElement.style.display === "none") {
                _xgateShowApiSection(controlElement);
            }
            _xgateSetHeaderElement(controlElement);
            _xgateHideHeaderSvg(controlElement);
            if (!document.getElementById("xgate-trial-container")) {
                _xgateInsertContainer(controlElement);
            }
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
    if (existing) {
        existing.parentNode.removeChild(existing);
    }
}