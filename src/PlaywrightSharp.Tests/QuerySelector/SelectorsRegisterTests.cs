using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>selectors.register</playwright-describe>
    public class SelectorsRegisterTests : PlaywrightSharpPageBaseTest
    {
        internal SelectorsRegisterTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var createTagSelector = () => ({
            name: 'tag',
        create(root, target)
          {
                    return target.nodeName;
                },
        query(root, selector)
          {
                    return root.querySelector(selector);
                },
        queryAll(root, selector)
          {
                    return Array.from(root.querySelectorAll(selector));
                }
            });
            await selectors.register($"({createTagSelector.ToString()})()");
            await Page.SetContentAsync('<div><span></span></div><div></div>');
            expect(await selectors._createSelector('tag', await Page.QuerySelectorAsync('div'))).toBe('DIV');
            expect(await Page.$eval('tag=DIV', e => e.nodeName)).toBe('DIV');
            expect(await Page.$eval('tag=SPAN', e => e.nodeName)).toBe('SPAN');
            expect(await Page.$$eval('tag=DIV', es => es.length)).toBe(2);

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should update</playwright-it>
        [Fact]
        public async Task ShouldUpdate()
        {
            await Page.SetContentAsync('<div><dummy id=d1></dummy></div><span><dummy id=d2></dummy></span>');
            expect(await Page.$eval('div', e => e.nodeName)).toBe('DIV');
            var error = await Page.QuerySelectorAsync('dummy=foo').catch (e => e);
            expect(error.message).toContain('Unknown engine dummy while parsing selector dummy=foo');
            var createDummySelector = (name) => ({
                name,
        create(root, target)
                {
                    return target.nodeName;
                },
        query(root, selector)
                {
                    return root.querySelector(name);
                },
        queryAll(root, selector)
                {
                    return Array.from(root.querySelectorAll(name));
                }
            });
            await selectors.register(createDummySelector, 'dummy');
            expect(await Page.$eval('dummy=foo', e => e.id)).toBe('d1');
            expect(await Page.$eval('css=span >> dummy=foo', e => e.id)).toBe('d2');

            }

        }

    }
}
