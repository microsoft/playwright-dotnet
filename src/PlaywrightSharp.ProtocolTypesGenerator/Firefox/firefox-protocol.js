const vm = require('vm');

function getJson(callback, protocolJSCode) {
    function inject() {
        this.ChromeUtils = {
            import: () => ({ t })
        }
        const t = {};
        t.String = { "$type": "string" };
        t.Number = { "$type": "number" };
        t.Boolean = { "$type": "boolean" };
        t.Undefined = { "$type": "undefined" };
        t.Any = { "$type": "any" };

        t.Enum = function (values) {
            return { "$type": "enum", "$values": values };
        }

        t.Nullable = function (scheme) {
            return { ...scheme, "$nullable": true };
        }

        t.Optional = function (scheme) {
            return { ...scheme, "$optional": true };
        }

        t.Array = function (scheme) {
            return { "$type": "array", "$items": scheme };
        }

        t.Recursive = function (types, schemeName) {
            return { "$type": "ref", "$ref": schemeName };
        }
    }

    const ctx = vm.createContext();
    const result = vm.runInContext(`(${inject})();${protocolJSCode}; this.protocol;`, ctx);
    callback(undefined, JSON.stringify(result));
}

module.exports = {
    getJson
};
