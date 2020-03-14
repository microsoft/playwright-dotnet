(/******/ (function (modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if (installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
            /******/
        }
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
            /******/
        };
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);/******//******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
        /******/
    }
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function (exports, name, getter) {
/******/ 		if (!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
            /******/
        }
        /******/
    };
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function (exports) {
/******/ 		if (typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
            /******/
        }
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
        /******/
    };
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function (value, mode) {
/******/ 		if (mode & 1) value = __webpack_require__(value);
/******/ 		if (mode & 8) return value;
/******/ 		if ((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if (mode & 2 && typeof value != 'string') for (var key in value) __webpack_require__.d(ns, key, function (key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
        /******/
    };
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function (module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
        /******/
    };
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function (object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = "./src/injected/injected.ts");
    /******/
})
/************************************************************************/
/******/({

/***/ "./src/injected/cssSelectorEngine.ts":
/*!*******************************************!*\\
  !*** ./src/injected/cssSelectorEngine.ts ***!
  \\*******************************************/
/*! no static exports found */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /**
             * Copyright (c) Microsoft Corporation.
             *
             * Licensed under the Apache License, Version 2.0 (the "License");
             * you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS,
             * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
             * See the License for the specific language governing permissions and
             * limitations under the License.
             */
            Object.defineProperty(exports, "__esModule", { value: true });
            exports.CSSEngine = {
                name: 'css',
                create(root, targetElement) {
                    const tokens = [];
                    function uniqueCSSSelector(prefix) {
                        const path = tokens.slice();
                        if (prefix)
                            path.unshift(prefix);
                        const selector = path.join(' > ');
                        const nodes = Array.from(root.querySelectorAll(selector));
                        return nodes[0] === targetElement ? selector : undefined;
                    }
                    for (let element = targetElement; element && element !== root; element = element.parentElement) {
                        const nodeName = element.nodeName.toLowerCase();
                        // Element ID is the strongest signal, use it.
                        let bestTokenForLevel = '';
                        if (element.id) {
                            const token = /^[a-zA-Z][a-zA-Z0-9\\-\\_]+$/.test(element.id) ? '#' + element.id : `[id="${element.id}"]`;
                            const selector = uniqueCSSSelector(token);
                            if (selector)
                                return selector;
                            bestTokenForLevel = token;
                        }
                        const parent = element.parentElement;
                        // Combine class names until unique.
                        const classes = Array.from(element.classList);
                        for (let i = 0; i < classes.length; ++i) {
                            const token = '.' + classes.slice(0, i + 1).join('.');
                            const selector = uniqueCSSSelector(token);
                            if (selector)
                                return selector;
                            // Even if not unique, does this subset of classes uniquely identify node as a child?
                            if (!bestTokenForLevel && parent) {
                                const sameClassSiblings = parent.querySelectorAll(token);
                                if (sameClassSiblings.length === 1)
                                    bestTokenForLevel = token;
                            }
                        }
                        // Ordinal is the weakest signal.
                        if (parent) {
                            const siblings = Array.from(parent.children);
                            const sameTagSiblings = siblings.filter(sibling => sibling.nodeName.toLowerCase() === nodeName);
                            const token = sameTagSiblings.length === 1 ? nodeName : `${nodeName}:nth-child(${1 + siblings.indexOf(element)})`;
                            const selector = uniqueCSSSelector(token);
                            if (selector)
                                return selector;
                            if (!bestTokenForLevel)
                                bestTokenForLevel = token;
                        }
                        else if (!bestTokenForLevel) {
                            bestTokenForLevel = nodeName;
                        }
                        tokens.unshift(bestTokenForLevel);
                    }
                    return uniqueCSSSelector();
                },
                query(root, selector) {
                    return root.querySelector(selector) || undefined;
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(selector));
                }
            };


            /***/
        }),

/***/ "./src/injected/injected.ts":
/*!**********************************!*\\
  !*** ./src/injected/injected.ts ***!
  \\**********************************/
/*! no static exports found */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /**
             * Copyright (c) Microsoft Corporation.
             *
             * Licensed under the Apache License, Version 2.0 (the "License");
             * you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS,
             * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
             * See the License for the specific language governing permissions and
             * limitations under the License.
             */
            Object.defineProperty(exports, "__esModule", { value: true });
            const utils_1 = __webpack_require__(/*! ./utils */ "./src/injected/utils.ts");
            const cssSelectorEngine_1 = __webpack_require__(/*! ./cssSelectorEngine */ "./src/injected/cssSelectorEngine.ts");
            const xpathSelectorEngine_1 = __webpack_require__(/*! ./xpathSelectorEngine */ "./src/injected/xpathSelectorEngine.ts");
            const textSelectorEngine_1 = __webpack_require__(/*! ./textSelectorEngine */ "./src/injected/textSelectorEngine.ts");
            function createAttributeEngine(attribute) {
                const engine = {
                    name: attribute,
                    create(root, target) {
                        const value = target.getAttribute(attribute);
                        if (!value)
                            return;
                        if (root.querySelector(`[${attribute}=${value}]`) === target)
                            return value;
                    },
                    query(root, selector) {
                        return root.querySelector(`[${attribute}=${selector}]`) || undefined;
                    },
                    queryAll(root, selector) {
                        return Array.from(root.querySelectorAll(`[${attribute}=${selector}]`));
                    }
                };
                return engine;
            }
            class Injected {
                constructor(customEngines) {
                    const defaultEngines = [
                        cssSelectorEngine_1.CSSEngine,
                        xpathSelectorEngine_1.XPathEngine,
                        textSelectorEngine_1.TextEngine,
                        createAttributeEngine('id'),
                        createAttributeEngine('data-testid'),
                        createAttributeEngine('data-test-id'),
                        createAttributeEngine('data-test'),
                    ];
                    this.utils = new utils_1.Utils();
                    this.engines = new Map();
                    for (const engine of [...defaultEngines, ...customEngines])
                        this.engines.set(engine.name, engine);
                }
                querySelector(selector, root) {
                    const parsed = this._parseSelector(selector);
                    if (!root['querySelector'])
                        throw new Error('Node is not queryable.');
                    let element = root;
                    for (const { engine, selector } of parsed) {
                        const next = engine.query(element.shadowRoot || element, selector);
                        if (!next)
                            return;
                        element = next;
                    }
                    return element;
                }
                querySelectorAll(selector, root) {
                    const parsed = this._parseSelector(selector);
                    if (!root['querySelectorAll'])
                        throw new Error('Node is not queryable.');
                    let set = new Set([root]);
                    for (const { engine, selector } of parsed) {
                        const newSet = new Set();
                        for (const prev of set) {
                            for (const next of engine.queryAll(prev.shadowRoot || prev, selector)) {
                                if (newSet.has(next))
                                    continue;
                                newSet.add(next);
                            }
                        }
                        set = newSet;
                    }
                    return Array.from(set);
                }
                _parseSelector(selector) {
                    let index = 0;
                    let quote;
                    let start = 0;
                    const result = [];
                    const append = () => {
                        const part = selector.substring(start, index);
                        const eqIndex = part.indexOf('=');
                        if (eqIndex === -1)
                            throw new Error(`Cannot parse selector ${selector}`);
                        const name = part.substring(0, eqIndex).trim();
                        const body = part.substring(eqIndex + 1);
                        const engine = this.engines.get(name.toLowerCase());
                        if (!engine)
                            throw new Error(`Unknown engine ${name} while parsing selector ${selector}`);
                        result.push({ engine, selector: body });
                    };
                    while (index < selector.length) {
                        const c = selector[index];
                        if (c === '\\\\' && index + 1 < selector.length) {
                            index += 2;
                        }
                        else if (c === quote) {
                            quote = undefined;
                            index++;
                        }
                        else if (!quote && c === '>' && selector[index + 1] === '>') {
                            append();
                            index += 2;
                            start = index;
                        }
                        else {
                            index++;
                        }
                    }
                    append();
                    return result;
                }
                isVisible(element) {
                    if (!element.ownerDocument || !element.ownerDocument.defaultView)
                        return true;
                    const style = element.ownerDocument.defaultView.getComputedStyle(element);
                    if (!style || style.visibility === 'hidden')
                        return false;
                    const rect = element.getBoundingClientRect();
                    return !!(rect.top || rect.bottom || rect.width || rect.height);
                }
                pollMutation(selector, predicate, timeout) {
                    let timedOut = false;
                    if (timeout)
                        setTimeout(() => timedOut = true, timeout);
                    const element = selector === undefined ? undefined : this.querySelector(selector, document);
                    const success = predicate(element);
                    if (success)
                        return Promise.resolve(success);
                    let fulfill;
                    const result = new Promise(x => fulfill = x);
                    const observer = new MutationObserver(() => {
                        if (timedOut) {
                            observer.disconnect();
                            fulfill();
                            return;
                        }
                        const element = selector === undefined ? undefined : this.querySelector(selector, document);
                        const success = predicate(element);
                        if (success) {
                            observer.disconnect();
                            fulfill(success);
                        }
                    });
                    observer.observe(document, {
                        childList: true,
                        subtree: true,
                        attributes: true
                    });
                    return result;
                }
                pollRaf(selector, predicate, timeout) {
                    let timedOut = false;
                    if (timeout)
                        setTimeout(() => timedOut = true, timeout);
                    let fulfill;
                    const result = new Promise(x => fulfill = x);
                    const onRaf = () => {
                        if (timedOut) {
                            fulfill();
                            return;
                        }
                        const element = selector === undefined ? undefined : this.querySelector(selector, document);
                        const success = predicate(element);
                        if (success)
                            fulfill(success);
                        else
                            requestAnimationFrame(onRaf);
                    };
                    onRaf();
                    return result;
                }
                pollInterval(selector, pollInterval, predicate, timeout) {
                    let timedOut = false;
                    if (timeout)
                        setTimeout(() => timedOut = true, timeout);
                    let fulfill;
                    const result = new Promise(x => fulfill = x);
                    const onTimeout = () => {
                        if (timedOut) {
                            fulfill();
                            return;
                        }
                        const element = selector === undefined ? undefined : this.querySelector(selector, document);
                        const success = predicate(element);
                        if (success)
                            fulfill(success);
                        else
                            setTimeout(onTimeout, pollInterval);
                    };
                    onTimeout();
                    return result;
                }
            }
            exports.default = Injected;


            /***/
        }),

/***/ "./src/injected/textSelectorEngine.ts":
/*!********************************************!*\\
  !*** ./src/injected/textSelectorEngine.ts ***!
  \\********************************************/
/*! no static exports found */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /**
             * Copyright (c) Microsoft Corporation.
             *
             * Licensed under the Apache License, Version 2.0 (the "License");
             * you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS,
             * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
             * See the License for the specific language governing permissions and
             * limitations under the License.
             */
            Object.defineProperty(exports, "__esModule", { value: true });
            exports.TextEngine = {
                name: 'text',
                create(root, targetElement, type) {
                    const document = root instanceof Document ? root : root.ownerDocument;
                    if (!document)
                        return;
                    for (let child = targetElement.firstChild; child; child = child.nextSibling) {
                        if (child.nodeType === 3 /* Node.TEXT_NODE */) {
                            const text = child.nodeValue;
                            if (!text)
                                continue;
                            if (text.match(/^\\s*[a-zA-Z0-9]+\\s*$/) && exports.TextEngine.query(root, text.trim()) === targetElement)
                                return text.trim();
                            if (exports.TextEngine.query(root, JSON.stringify(text)) === targetElement)
                                return JSON.stringify(text);
                        }
                    }
                },
                query(root, selector) {
                    const document = root instanceof Document ? root : root.ownerDocument;
                    if (!document)
                        return;
                    const matcher = createMatcher(selector);
                    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
                    while (walker.nextNode()) {
                        const node = walker.currentNode;
                        const element = node.parentElement;
                        const text = node.nodeValue;
                        if (element && text && matcher(text))
                            return element;
                    }
                },
                queryAll(root, selector) {
                    const result = [];
                    const document = root instanceof Document ? root : root.ownerDocument;
                    if (!document)
                        return result;
                    const matcher = createMatcher(selector);
                    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
                    while (walker.nextNode()) {
                        const node = walker.currentNode;
                        const element = node.parentElement;
                        const text = node.nodeValue;
                        if (element && text && matcher(text))
                            result.push(element);
                    }
                    return result;
                }
            };
            function createMatcher(selector) {
                if (selector[0] === '"' && selector[selector.length - 1] === '"') {
                    const parsed = JSON.parse(selector);
                    return text => text === parsed;
                }
                if (selector[0] === '/' && selector.lastIndexOf('/') > 0) {
                    const lastSlash = selector.lastIndexOf('/');
                    const re = new RegExp(selector.substring(1, lastSlash), selector.substring(lastSlash + 1));
                    return text => re.test(text);
                }
                selector = selector.trim();
                return text => text.trim() === selector;
            }


            /***/
        }),

/***/ "./src/injected/utils.ts":
/*!*******************************!*\\
  !*** ./src/injected/utils.ts ***!
  \\*******************************/
/*! no static exports found */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /**
             * Copyright (c) Microsoft Corporation.
             *
             * Licensed under the Apache License, Version 2.0 (the "License");
             * you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS,
             * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
             * See the License for the specific language governing permissions and
             * limitations under the License.
             */
            Object.defineProperty(exports, "__esModule", { value: true });
            class Utils {
                parentElementOrShadowHost(element) {
                    if (element.parentElement)
                        return element.parentElement;
                    if (!element.parentNode)
                        return;
                    if (element.parentNode.nodeType === Node.DOCUMENT_FRAGMENT_NODE && element.parentNode.host)
                        return element.parentNode.host;
                }
                deepElementFromPoint(document, x, y) {
                    let container = document;
                    let element;
                    while (container) {
                        const innerElement = container.elementFromPoint(x, y);
                        if (!innerElement || element === innerElement)
                            break;
                        element = innerElement;
                        container = element.shadowRoot;
                    }
                    return element;
                }
            }
            exports.Utils = Utils;


            /***/
        }),

/***/ "./src/injected/xpathSelectorEngine.ts":
/*!*********************************************!*\\
  !*** ./src/injected/xpathSelectorEngine.ts ***!
  \\*********************************************/
/*! no static exports found */
/***/ (function (module, exports, __webpack_require__) {

            "use strict";

            /**
             * Copyright (c) Microsoft Corporation.
             *
             * Licensed under the Apache License, Version 2.0 (the "License");
             * you may not use this file except in compliance with the License.
             * You may obtain a copy of the License at
             *
             * http://www.apache.org/licenses/LICENSE-2.0
             *
             * Unless required by applicable law or agreed to in writing, software
             * distributed under the License is distributed on an "AS IS" BASIS,
             * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
             * See the License for the specific language governing permissions and
             * limitations under the License.
             */
            Object.defineProperty(exports, "__esModule", { value: true });
            const maxTextLength = 80;
            const minMeaningfulSelectorLegth = 100;
            exports.XPathEngine = {
                name: 'xpath',
                create(root, targetElement, type) {
                    const maybeDocument = root instanceof Document ? root : root.ownerDocument;
                    if (!maybeDocument)
                        return;
                    const document = maybeDocument;
                    const xpathCache = new Map();
                    if (type === 'notext')
                        return createNoText(root, targetElement);
                    const tokens = [];
                    function evaluateXPath(expression) {
                        let nodes = xpathCache.get(expression);
                        if (!nodes) {
                            nodes = [];
                            try {
                                const result = document.evaluate(expression, root, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE);
                                for (let node = result.iterateNext(); node; node = result.iterateNext()) {
                                    if (node.nodeType === Node.ELEMENT_NODE)
                                        nodes.push(node);
                                }
                            }
                            catch (e) {
                            }
                            xpathCache.set(expression, nodes);
                        }
                        return nodes;
                    }
                    function uniqueXPathSelector(prefix) {
                        const path = tokens.slice();
                        if (prefix)
                            path.unshift(prefix);
                        let selector = '//' + path.join('/');
                        while (selector.includes('///'))
                            selector = selector.replace('///', '//');
                        if (selector.endsWith('/'))
                            selector = selector.substring(0, selector.length - 1);
                        const nodes = evaluateXPath(selector);
                        if (nodes[nodes.length - 1] === targetElement)
                            return selector;
                        // If we are looking at a small set of elements with long selector, fall back to ordinal.
                        if (nodes.length < 5 && selector.length > minMeaningfulSelectorLegth) {
                            const index = nodes.indexOf(targetElement);
                            if (index !== -1)
                                return `(${selector})[${index + 1}]`;
                        }
                        return undefined;
                    }
                    function escapeAndCap(text) {
                        text = text.substring(0, maxTextLength);
                        // XPath 1.0 does not support quote escaping.
                        // 1. If there are no single quotes - use them.
                        if (text.indexOf(`'`) === -1)
                            return `'${text}'`;
                        // 2. If there are no double quotes - use them to enclose text.
                        if (text.indexOf(`"`) === -1)
                            return `"${text}"`;
                        // 3. Otherwise, use popular |concat| trick.
                        const Q = `'`;
                        return `concat(${text.split(Q).map(token => Q + token + Q).join(`, "'", `)})`;
                    }
                    const defaultAttributes = new Set(['title', 'aria-label', 'disabled', 'role']);
                    const importantAttributes = new Map([
                        ['form', ['action']],
                        ['img', ['alt']],
                        ['input', ['placeholder', 'type', 'name', 'value']],
                    ]);
                    let usedTextConditions = false;
                    for (let element = targetElement; element && element !== root; element = element.parentElement) {
                        const nodeName = element.nodeName.toLowerCase();
                        const tag = nodeName === 'svg' ? '*' : nodeName;
                        const tagConditions = [];
                        if (nodeName === 'svg')
                            tagConditions.push('local-name()="svg"');
                        const attrConditions = [];
                        const importantAttrs = [...defaultAttributes, ...(importantAttributes.get(tag) || [])];
                        for (const attr of importantAttrs) {
                            const value = element.getAttribute(attr);
                            if (value && value.length < maxTextLength)
                                attrConditions.push(`normalize-space(@${attr})=${escapeAndCap(value)}`);
                            else if (value)
                                attrConditions.push(`starts-with(normalize-space(@${attr}), ${escapeAndCap(value)})`);
                        }
                        const text = document.evaluate('normalize-space(.)', element).stringValue;
                        const textConditions = [];
                        if (tag !== 'select' && text.length && !usedTextConditions) {
                            if (text.length < maxTextLength)
                                textConditions.push(`normalize-space(.)=${escapeAndCap(text)}`);
                            else
                                textConditions.push(`starts-with(normalize-space(.), ${escapeAndCap(text)})`);
                            usedTextConditions = true;
                        }
                        // Always retain the last tag.
                        const conditions = [...tagConditions, ...textConditions, ...attrConditions];
                        const token = conditions.length ? `${tag}[${conditions.join(' and ')}]` : (tokens.length ? '' : tag);
                        const selector = uniqueXPathSelector(token);
                        if (selector)
                            return selector;
                        // Ordinal is the weakest signal.
                        const parent = element.parentElement;
                        let tagWithOrdinal = tag;
                        if (parent) {
                            const siblings = Array.from(parent.children);
                            const sameTagSiblings = siblings.filter(sibling => sibling.nodeName.toLowerCase() === nodeName);
                            if (sameTagSiblings.length > 1)
                                tagWithOrdinal += `[${1 + siblings.indexOf(element)}]`;
                        }
                        // Do not include text into this token, only tag / attributes.
                        // Topmost node will get all the text.
                        const nonTextConditions = [...tagConditions, ...attrConditions];
                        const levelToken = nonTextConditions.length ? `${tagWithOrdinal}[${nonTextConditions.join(' and ')}]` : tokens.length ? '' : tagWithOrdinal;
                        tokens.unshift(levelToken);
                    }
                    return uniqueXPathSelector();
                },
                query(root, selector) {
                    const document = root instanceof Document ? root : root.ownerDocument;
                    if (!document)
                        return;
                    const it = document.evaluate(selector, root, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE);
                    for (let node = it.iterateNext(); node; node = it.iterateNext()) {
                        if (node.nodeType === Node.ELEMENT_NODE)
                            return node;
                    }
                },
                queryAll(root, selector) {
                    const result = [];
                    const document = root instanceof Document ? root : root.ownerDocument;
                    if (!document)
                        return result;
                    const it = document.evaluate(selector, root, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE);
                    for (let node = it.iterateNext(); node; node = it.iterateNext()) {
                        if (node.nodeType === Node.ELEMENT_NODE)
                            result.push(node);
                    }
                    return result;
                }
            };
            function createNoText(root, targetElement) {
                const steps = [];
                for (let element = targetElement; element && element !== root; element = element.parentElement) {
                    if (element.getAttribute('id')) {
                        steps.unshift(`//*[@id="${element.getAttribute('id')}"]`);
                        return steps.join('/');
                    }
                    const siblings = element.parentElement ? Array.from(element.parentElement.children) : [];
                    const similarElements = siblings.filter(sibling => element.nodeName === sibling.nodeName);
                    const index = similarElements.length === 1 ? 0 : similarElements.indexOf(element) + 1;
                    steps.unshift(index ? `${element.nodeName}[${index}]` : element.nodeName);
                }
                return '/' + steps.join('/');
            }


            /***/
        })

    /******/
})).default