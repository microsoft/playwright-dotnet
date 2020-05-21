using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol.Page;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumPdf
    {
        private static readonly Dictionary<string, double> _unitToPixels = new Dictionary<string, double>
        {
            ["px"] = 1,
            ["in"] = 96,
            ["cm"] = 37.8,
            ["mm"] = 3.78,
        };

        private readonly ChromiumSession _client;

        public ChromiumPdf(ChromiumSession client)
        {
            _client = client;
        }

        public async Task<byte[]> GenerateAsync(string file, PdfOptions options)
        {
            double paperWidth = PaperFormat.Letter.Width;
            double paperHeight = PaperFormat.Letter.Height;

            if (options.Format != null)
            {
                paperWidth = options.Format.Width;
                paperHeight = options.Format.Height;
            }
            else
            {
                if (options.Width != null)
                {
                    paperWidth = ConvertPrintParameterToInches(options.Width);
                }

                if (options.Height != null)
                {
                    paperHeight = ConvertPrintParameterToInches(options.Height);
                }
            }

            double marginTop = ConvertPrintParameterToInches(options.MarginOptions.Top);
            double marginLeft = ConvertPrintParameterToInches(options.MarginOptions.Left);
            double marginBottom = ConvertPrintParameterToInches(options.MarginOptions.Bottom);
            double marginRight = ConvertPrintParameterToInches(options.MarginOptions.Right);

            var result = await _client.SendAsync(new PagePrintToPDFRequest
            {
                TransferMode = "ReturnAsStream",
                Landscape = options.Landscape,
                DisplayHeaderFooter = options.DisplayHeaderFooter,
                HeaderTemplate = options.HeaderTemplate,
                FooterTemplate = options.FooterTemplate,
                PrintBackground = options.PrintBackground,
                Scale = options.Scale,
                PaperWidth = paperWidth,
                PaperHeight = paperHeight,
                MarginTop = marginTop,
                MarginBottom = marginBottom,
                MarginLeft = marginLeft,
                MarginRight = marginRight,
                PageRanges = options.PageRanges,
                PreferCSSPageSize = options.PreferCSSPageSize,
            }).ConfigureAwait(false);

            return await ProtocolStreamReader.ReadProtocolStreamByteAsync(_client, result.Stream, file).ConfigureAwait(false);
        }

        private double ConvertPrintParameterToInches(object parameter)
        {
            if (parameter == null)
            {
                return 0;
            }

            double pixels;
            if (parameter is double || parameter is int)
            {
                pixels = Convert.ToDouble(parameter);
            }
            else
            {
                string text = parameter.ToString();
                string unit = text.Substring(text.Length - 2).ToLower();
                string valueText;
                if (_unitToPixels.ContainsKey(unit))
                {
                    valueText = text.Substring(0, text.Length - 2);
                }
                else
                {
                    // In case of unknown unit try to parse the whole parameter as number of pixels.
                    // This is consistent with phantom's paperSize behavior.
                    unit = "px";
                    valueText = text;
                }

                if (double.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out double number))
                {
                    pixels = number * _unitToPixels[unit];
                }
                else
                {
                    throw new ArgumentException($"Failed to parse parameter value: '{text}'", nameof(parameter));
                }
            }

            return pixels / 96;
        }
    }
}
