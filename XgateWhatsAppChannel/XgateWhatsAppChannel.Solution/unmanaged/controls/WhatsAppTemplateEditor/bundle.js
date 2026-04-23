/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./WhatsAppTemplateEditor/HelloWorld.tsx"
/*!***********************************************!*\
  !*** ./WhatsAppTemplateEditor/HelloWorld.tsx ***!
  \***********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   HelloWorld: () => (/* binding */ HelloWorld)\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n\nclass HelloWorld extends react__WEBPACK_IMPORTED_MODULE_0__.Component {\n  constructor(props) {\n    super(props);\n    this.fetchTemplateDetails = () => {\n      var {\n        templateIdInput\n      } = this.state;\n      if (!templateIdInput) return;\n      this.setState({\n        isLoading: true,\n        variables: [],\n        userInputs: {}\n      });\n      // 1. 构造 Custom API 的请求体\n      var request = {\n        TemplateId: templateIdInput,\n        getMetadata: function getMetadata() {\n          return {\n            boundParameter: null,\n            parameterTypes: {\n              \"TemplateId\": {\n                typeName: \"Edm.String\",\n                structuralProperty: 1\n              }\n            },\n            operationType: 0,\n            operationName: \"xgate_GetWhatsAppTemplate\"\n          };\n        }\n      };\n      // 2. 【修复 ESLint 报错的核心】为全局 window 定义精确的 Xrm 接口类型，彻底消灭 any\n      var globalWindow = window;\n      var Xrm = globalWindow.Xrm;\n      if (!(Xrm === null || Xrm === void 0 ? void 0 : Xrm.WebApi)) {\n        alert(\"请将控件部署到 D365 环境中测试网络请求。本地测试台不支持 Xrm.WebApi。\");\n        this.setState({\n          isLoading: false\n        });\n        return;\n      }\n      // 3. 使用标准的 Response 和 Error 类型，替换掉之前的 any\n      Xrm.WebApi.online.execute(request).then(response => {\n        if (response.ok) {\n          // 强制声明返回值的类型，解决 unsafe-assignment 报错\n          return response.json();\n        } else {\n          throw new Error(response.statusText);\n        }\n      }).then(responseBody => {\n        var _a;\n        // 安全地提取字符串\n        var templateData = responseBody.TemplateDataJson;\n        var rawJsonString = typeof templateData === \"string\" ? templateData : \"{}\";\n        console.log(\"C# 返回的原始数据:\", rawJsonString);\n        try {\n          // 安全地解析 JSON\n          var parsedData = JSON.parse(rawJsonString);\n          if (parsedData.code === 0 || !parsedData.error) {\n            // 【你需要根据真实的 JSON 结构调整这里】\n            // 假设真实结构是：{ data: { variables: [\"shopName\", \"salesName\"] } }\n            var dataObj = parsedData.data;\n            var fetchedVariables = (_a = dataObj === null || dataObj === void 0 ? void 0 : dataObj.variables) !== null && _a !== void 0 ? _a : [];\n            this.setState({\n              isLoading: false,\n              variables: fetchedVariables\n            });\n          } else {\n            var errorMessage = typeof parsedData.message === \"string\" ? parsedData.message : \"未知错误\";\n            alert(\"\\u540E\\u7AEF\\u63A5\\u53E3\\u62A5\\u9519: \".concat(errorMessage));\n            this.setState({\n              isLoading: false\n            });\n          }\n        } catch (e) {\n          console.error(\"JSON 解析失败:\", e);\n          this.setState({\n            isLoading: false\n          });\n        }\n        return null;\n      }).catch(error => {\n        console.error(\"调用 Custom API 失败:\", error);\n        alert(\"调用后端代理接口失败，请检查网络或插件报错日志。\");\n        this.setState({\n          isLoading: false\n        });\n      });\n    };\n    this.handleInputChange = (variableName, value) => {\n      this.setState(prevState => {\n        var newUserInputs = Object.assign(Object.assign({}, prevState.userInputs), {\n          [variableName]: value\n        });\n        // 【关键逻辑】触发回调，把最新的 JSON 字符串交给 index.ts\n        if (this.props.onDataChanged) {\n          this.props.onDataChanged(JSON.stringify(newUserInputs));\n        }\n        return {\n          userInputs: newUserInputs\n        };\n      });\n    };\n    // 初始化状态\n    this.state = {\n      templateIdInput: '',\n      isLoading: false,\n      variables: [],\n      userInputs: {}\n    };\n  }\n  render() {\n    var {\n      templateIdInput,\n      isLoading,\n      variables,\n      userInputs\n    } = this.state;\n    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n      style: {\n        padding: '20px',\n        fontFamily: 'Segoe UI, sans-serif'\n      }\n    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"h3\", {\n      style: {\n        color: '#25D366',\n        borderBottom: '2px solid #25D366',\n        paddingBottom: '10px'\n      }\n    }, \"\\uD83D\\uDCAC WhatsApp \\u52A8\\u6001\\u6A21\\u677F\\u914D\\u7F6E\"), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n      style: {\n        marginBottom: '20px'\n      }\n    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"label\", {\n      style: {\n        fontWeight: 'bold',\n        display: 'block',\n        marginBottom: '5px'\n      }\n    }, \"Template ID:\"), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"input\", {\n      type: \"text\",\n      value: templateIdInput,\n      onChange: e => this.setState({\n        templateIdInput: e.target.value\n      }),\n      placeholder: \"\\u8F93\\u5165 1002015 \\u6216 1002016 \\u6D4B\\u8BD5\",\n      style: {\n        padding: '8px',\n        width: '200px',\n        marginRight: '10px',\n        borderRadius: '4px',\n        border: '1px solid #ccc'\n      }\n    }), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"button\", {\n      onClick: this.fetchTemplateDetails,\n      disabled: isLoading,\n      style: {\n        padding: '8px 15px',\n        backgroundColor: '#25D366',\n        color: 'white',\n        border: 'none',\n        borderRadius: '4px',\n        cursor: 'pointer'\n      }\n    }, isLoading ? '拉取中...' : '获取模板变量')), variables.length > 0 && (/*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n      style: {\n        padding: '15px',\n        backgroundColor: '#f9f9f9',\n        borderRadius: '8px',\n        border: '1px solid #e0e0e0'\n      }\n    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"h4\", {\n      style: {\n        marginTop: 0\n      }\n    }, \"\\u9700\\u8981\\u914D\\u7F6E\\u7684\\u53D8\\u91CF (\", variables.length, \" \\u4E2A):\"), variables.map(varName => (/*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n      key: varName,\n      style: {\n        marginBottom: '10px'\n      }\n    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"label\", {\n      style: {\n        display: 'inline-block',\n        width: '120px',\n        color: '#555'\n      }\n    }, varName, \":\"), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"input\", {\n      type: \"text\",\n      value: userInputs[varName] || '',\n      onChange: e => this.handleInputChange(varName, e.target.value),\n      placeholder: \"\\u8BF7\\u8F93\\u5165 \".concat(varName, \" \\u7684\\u503C\"),\n      style: {\n        padding: '6px',\n        width: '250px',\n        borderRadius: '4px',\n        border: '1px solid #ccc'\n      }\n    })))))), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"div\", {\n      style: {\n        marginTop: '20px',\n        padding: '10px',\n        backgroundColor: '#2d2d2d',\n        color: '#85c46c',\n        borderRadius: '4px',\n        fontFamily: 'monospace'\n      }\n    }, /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"strong\", null, \"\\u5B9E\\u65F6\\u7EC4\\u88C5\\u7684 JSON (\\u5C06\\u4F20\\u7ED9 C# \\u540E\\u7AEF):\"), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(\"br\", null), JSON.stringify(userInputs, null, 2)));\n  }\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./WhatsAppTemplateEditor/HelloWorld.tsx?\n}");

/***/ },

/***/ "./WhatsAppTemplateEditor/index.ts"
/*!*****************************************!*\
  !*** ./WhatsAppTemplateEditor/index.ts ***!
  \*****************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

eval("{__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   WhatsAppTemplateEditor: () => (/* binding */ WhatsAppTemplateEditor)\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n/* harmony import */ var _HelloWorld__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./HelloWorld */ \"./WhatsAppTemplateEditor/HelloWorld.tsx\");\n\n// 【删除】不再需要 import * as ReactDOM from \"react-dom\";\n\n// 【修改】实现接口改为 ReactControl\nclass WhatsAppTemplateEditor {\n  constructor() {\n    /**\n     * 3. 我们自定义的回调：接收 React 传来的最新 JSON\n     */\n    this.handleReactDataChange = newJsonString => {\n      this.currentJsonPayload = newJsonString;\n      this.notifyOutputChanged(); // 敲响 D365 的警钟\n    };\n  }\n  /**\n   * 1. 控件初始化\n   */\n  init(context, notifyOutputChanged, state) {\n    var _a;\n    this.notifyOutputChanged = notifyOutputChanged;\n    // 记录初始值\n    this.currentJsonPayload = (_a = context.parameters.sampleProperty.raw) !== null && _a !== void 0 ? _a : \"\";\n  }\n  /**\n   * 2. 渲染视图：当数据变化时触发\n   */\n  updateView(context) {\n    var _a;\n    // 准备传给 React 组件的属性\n    var props = {\n      name: (_a = context.parameters.sampleProperty.raw) !== null && _a !== void 0 ? _a : \"\",\n      onDataChanged: this.handleReactDataChange\n    };\n    // 【关键变化】直接返回 React 元素，D365 引擎会自动帮我们把它挂载到页面上！\n    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0__.createElement(_HelloWorld__WEBPACK_IMPORTED_MODULE_1__.HelloWorld, props);\n  }\n  /**\n   * 4. D365 听到警钟后来拿数据\n   */\n  getOutputs() {\n    var _a;\n    return {\n      sampleProperty: (_a = this.currentJsonPayload) !== null && _a !== void 0 ? _a : \"\"\n    };\n  }\n  /**\n   * 5. 资源清理\n   */\n  destroy() {\n    // 在 ReactControl 模式下，DOM 的清理和组件卸载完全由 D365 引擎自动接管，不需要我们写代码\n  }\n}\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./WhatsAppTemplateEditor/index.ts?\n}");

/***/ },

/***/ "react"
/*!***************************!*\
  !*** external "Reactv16" ***!
  \***************************/
(module) {

module.exports = Reactv16;

/***/ }

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		if (!(moduleId in __webpack_modules__)) {
/******/ 			delete __webpack_module_cache__[moduleId];
/******/ 			var e = new Error("Cannot find module '" + moduleId + "'");
/******/ 			e.code = 'MODULE_NOT_FOUND';
/******/ 			throw e;
/******/ 		}
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/compat get default export */
/******/ 	(() => {
/******/ 		// getDefaultExport function for compatibility with non-harmony modules
/******/ 		__webpack_require__.n = (module) => {
/******/ 			var getter = module && module.__esModule ?
/******/ 				() => (module['default']) :
/******/ 				() => (module);
/******/ 			__webpack_require__.d(getter, { a: getter });
/******/ 			return getter;
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	var __webpack_exports__ = __webpack_require__("./WhatsAppTemplateEditor/index.ts");
/******/ 	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = __webpack_exports__;
/******/ 	
/******/ })()
;
if (window.ComponentFramework && window.ComponentFramework.registerControl) {
	ComponentFramework.registerControl('XgateUI.WhatsAppTemplateEditor', pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.WhatsAppTemplateEditor);
} else {
	var XgateUI = XgateUI || {};
	XgateUI.WhatsAppTemplateEditor = pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.WhatsAppTemplateEditor;
	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = undefined;
}