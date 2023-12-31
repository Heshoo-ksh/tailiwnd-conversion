﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace tailiwnd_conversion
{
    internal class Program
    {
        private static string inputFilePath = @"C:\Codes\Epitec Finance\Epitec.Finance.Web\src\app\shared\edit-factor-value-modal\edit-factor-value-modal.component.html";
        //private static string outputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-details-processed.component.html";
        private static string outputFilePath = inputFilePath;

        private static bool shouldAddFlex = true; // The flag that controls whether "flex" should be added
        private static int nextOrderValue = 1;
        private static Dictionary<string, Func<string, string>> 
            conversionDictionary = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"fxLayout", ConvertFxLayout},
                {"fxLayoutGap", ConvertFxLayoutGap},
                {"fxLayoutAlign", ConvertFxLayoutAlign},
                {"fxFlex", ConvertFxFlex},
                {"gdColumns", ConvertGdColumns},
                {"fxFlexOrder", ConvertFxFlexOrder},
               // {"fxFlexAlign", ConvertFxFlexAlign},
                {"fxFlexFill", _ => { shouldAddFlex = false; return  "fill"; }},
                {"fxFill", _ => { shouldAddFlex = false; return  "fill"; }},

            };
        
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var html = File.ReadAllText(inputFilePath);
            var doc = new HtmlDocument();
            doc.OptionOutputOriginalCase = true;
            doc.OptionWriteEmptyNodes = false;
            doc.LoadHtml(html);

            ConvertAttributesToClasses(doc);
            ReorderClassesForTailwindConvention(doc);
            ReorderClassesForMobileFirst(doc);
            CleanClassAttributes(doc);

            SaveHtml(doc, outputFilePath);

            Console.WriteLine($"HTML conversion completed! Output written to {outputFilePath}");
           // Console.ReadLine();
        }

        private static void ConvertAttributesToClasses(HtmlDocument doc)
        {
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                foreach (var attribute in node.Attributes.ToList()) 
                {
                    shouldAddFlex = true; // Reset flag for each node

                    var existingClasses = new HashSet<string>(node.GetAttributeValue("class", "").Split(' '));

                    var (coreProperty, tailwindPrefix) = ExtractPropertyAndPrefix(attribute.Name);


                    if (conversionDictionary.TryGetValue(coreProperty, out var converterFunc))
                    {
                        var newClass = "";
                        
                        if (coreProperty == "fxflexorder")
                        {
                            newClass = converterFunc(attribute.Name + "|||" + attribute.Value);
                        }
                        else
                        {
                            newClass = converterFunc(attribute.Value);
                        }

                        if (shouldAddFlex && !existingClasses.Contains("flex") && !existingClasses.Contains("grid"))
                        {
                            existingClasses.Add("flex");
                        }
                        existingClasses.Add(tailwindPrefix + newClass);
                        node.Attributes.Remove(attribute);

                        EnsureGridFlowRowAndOrder(existingClasses);

                        node.SetAttributeValue("class", string.Join(" ", existingClasses.ToArray()));
                    }
                }
            }
        }

        private static void ReorderClassesForTailwindConvention(HtmlDocument doc)
        {
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.HasAttributes && node.Attributes["class"] != null)
                {
                    var classes = node.Attributes["class"].Value.Split(' ').ToList();

                    classes.Sort(CompareTailwindClasses);

                    node.Attributes["class"].Value = string.Join(" ", classes);
                }
            }
        }

        private static int CompareTailwindClasses(string a, string b)
        {
            // Placeholders for checking the category of each class
            bool isACustom = !a.StartsWith("mat-") && !a.StartsWith("flex") && !a.StartsWith("grid") && !a.StartsWith("gap-") && !a.StartsWith("grid-cols") && !a.StartsWith("justify-") && !a.StartsWith("items-");
            bool isBCustom = !b.StartsWith("mat-") && !b.StartsWith("flex") && !b.StartsWith("grid") && !b.StartsWith("gap-") && !b.StartsWith("grid-cols") && !b.StartsWith("justify-") && !b.StartsWith("items-");
            bool isAMat = a.StartsWith("mat-");
            bool isBMat = b.StartsWith("mat-");
            bool isAFlex = a == "flex";
            bool isBFlex = b == "flex";
            bool isAGrid = a == "grid";
            bool isBGrid = b == "grid";
            bool isAGridFlowRow = a.StartsWith("grid-flow-row");
            bool isBGridFlowRow = b.StartsWith("grid-flow-row");
            bool isAGap = a.StartsWith("gap-");
            bool isBGap = b.StartsWith("gap-");
            bool isAGridCols = a.StartsWith("grid-cols");
            bool isBGridCols = b.StartsWith("grid-cols");

            // Custom classes should come first
            if (isACustom) return -1;
            if (isBCustom) return 1;

            // mat- prefixed classes should come after custom classes
            if (isAMat && !isBCustom) return -1;
            if (isBMat && !isACustom) return 1;

            // TailwindCSS classes
            // Ensure 'flex' comes first among Tailwind classes
            if (isAFlex) return -1;
            if (isBFlex) return 1;

            // Ensure 'grid' comes second after 'flex'
            if (isAGrid && !isBFlex) return -1;
            if (isBGrid && !isAFlex) return 1;

            // Ensure 'grid-flow-row' comes third after 'grid'
            if (isAGridFlowRow && !isBGrid && !isBFlex) return -1;
            if (isBGridFlowRow && !isAGrid && !isAFlex) return 1;

            // Ensure 'gap-' comes fourth after 'grid-flow-row'
            if (isAGap && !isBGridFlowRow && !isBGrid && !isBFlex) return -1;
            if (isBGap && !isAGridFlowRow && !isAGrid && !isAFlex) return 1;

            // 'grid-cols' should come last among Tailwind classes
            if (isAGridCols) return 1;
            if (isBGridCols) return -1;

            return 0; // Default case (should ideally not occur)
            /*// Ensure 'flex' comes first
    if (a == "flex") return -1;
    if (b == "flex") return 1;

    // Ensure 'grid' comes second after 'flex'
    if (a == "grid" && b != "flex") return -1;
    if (b == "grid" && a != "flex") return 1;

    // Ensure 'grid-flow-row' comes third after 'grid'
    if (a.StartsWith("grid-flow-row") && b != "grid" && b != "flex") return -1;
    if (b.StartsWith("grid-flow-row") && a != "grid" && a != "flex") return 1;

    // Ensure 'gap-' comes fourth after 'grid-flow-row'
    if (a.StartsWith("gap-") && !b.StartsWith("grid-cols") && b != "grid-flow-row" && b != "grid" && b != "flex") return -1;
    if (b.StartsWith("gap-") && !a.StartsWith("grid-cols") && a != "grid-flow-row" && a != "grid" && a != "flex") return 1;

    // 'grid-cols' should come last
    if (a.StartsWith("grid-cols")) return 1;
    if (b.StartsWith("grid-cols")) return -1;

    return 0; // Fallback condition if no rules apply*/
        }

        private static void ReorderClassesForMobileFirst(HtmlDocument doc)
        {
            var breakpoints = new List<string> { "base", "sm", "md", "lg", "xl" };

            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.HasAttributes && node.Attributes["class"] != null)
                {
                    var classes = node.Attributes["class"].Value.Split(' ').ToList();
                    classes = classes.OrderBy(c => GetOrderForClass(c, breakpoints)).ToList();
                    node.SetAttributeValue("class", string.Join(" ", classes));
                }
            }
        }
        
        private static void CleanClassAttributes(HtmlDocument doc)
        {
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.HasAttributes && node.Attributes["class"] != null)
                {
                    node.Attributes["class"].Value = node.Attributes["class"].Value.Trim();
                }
            }
        }

        private static void SaveHtml(HtmlDocument doc, string path)
        {
            var output = doc.DocumentNode.OuterHtml;
            File.WriteAllText(path, output);

            var outputContent = File.ReadAllText(path);
            outputContent = outputContent.Replace("=\"\"", "");
            File.WriteAllText(path, outputContent);
        }


        //----not called in main----//
        private static (string CoreProperty, string TailwindPrefix) ExtractPropertyAndPrefix(string attributeName)
        {
            var parts = attributeName.Split('.');
            var coreProperty = parts[0];
            var prefixSize = parts.Length > 1 ? parts[1] : "";
            var tailwindPrefix = ConvertScreenSize(prefixSize);
            return (coreProperty, tailwindPrefix);
        }
        private static int GetOrderForClass(string className, List<string> breakpoints)
        {
            for (int i = 0; i < breakpoints.Count; i++)
            {
                if (className.StartsWith($"{breakpoints[i]}:"))
                {
                    return i;
                }
            }
            return 0;  // default is 'base'
        }  

        private static string ConvertScreenSize(string prefixSize)
        {
            if (string.IsNullOrEmpty(prefixSize))
                return "";

            var parts = prefixSize.Split('-');
            var prefix = parts.Length > 1 ? parts[0] : "";
            var size = parts.Length > 1 ? parts[1] : parts[0];

            if (string.IsNullOrEmpty(prefix))
            {
                // If no prefix, return the screen size directly
                if (size == "xs" || size == "sm")
                    return "";
                return size + ":";
            }

            string screenSize = "";
            switch (size)
            {
                case "xs":
                    screenSize = (prefix == "gt") ? "sm:" : "";
                    break;
                case "sm":
                    screenSize = (prefix == "gt") ? "md:" : "";
                    break;
                case "md":
                    screenSize = (prefix == "gt") ? "lg:" : "sm:";
                    break;
                case "lg":
                    screenSize = (prefix == "gt") ? "xl:" : "md:";
                    break;
                case "xl":
                    screenSize = "lg:"; // lt-xl maps to lg:
                    break;
            }
            return screenSize;
        }
    
        private static void EnsureGridFlowRowAndOrder(HashSet<string> existingClasses)
        {
            // Ensure grid-flow-row is present if grid-cols-* is present
            if (existingClasses.Any(c => c.StartsWith("grid-cols-")) && !existingClasses.Contains("grid-flow-row"))
            {
                existingClasses.Add("grid-flow-row");
            }
            if (existingClasses.Any(c => c.StartsWith("grid-cols-")) && !existingClasses.Contains("grid "))
            {
                existingClasses.Add("grid");
            }
        }
        
        //-----conversion functions
        private static string ConvertFxLayout(string value)
        {
            shouldAddFlex = false;

            if (value == "row")
                return "flex";
            else if (value == "column")
                return "flex-col";
            return "";
        }

        private static string ConvertFxLayoutGap(string value)
        {
            // Split the value into parts
            var parts = value.Split(' ').Select(part => part.Trim()).ToList();

            // Extract gap values
            string gapX = "";
            string gapY = "";

            if (parts.Count == 1)
            {
                gapX = $"gap-[{parts[0]}]";
            }
            else if (parts.Count > 1)
            {
                gapX = parts[0] != "0px" ? $"gap-x-[{parts[0]}]" : "";
                gapY = parts[1] != "0px" ? $"gap-y-[{parts[1]}]" : "";
            }

            return $"{gapX} {gapY}".Trim();
        }

        private static string ConvertFxFlex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            value = value.Trim(); 

            if (value.Contains(" "))
            {
                var parts = value.Split(' ');

                if (parts.Length == 3)
                {
                    // Only add the flex-grow class if the value is not 0
                    string flexGrowClass = parts[0] != "0" ? "flex-grow" : "";

                    // Only add the flex-shrink class if the value is not 0
                    string flexShrinkClass = parts[1] != "0" ? "flex-shrink" : "";

                    return $"{flexGrowClass} {flexShrinkClass} flex-basis-[{parts[2]}] max-w-[{parts[2]}]".Trim();
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return $"basis-[{value}%]";
            }
        }


        private static string ConvertGdColumns(string value)
        {
            shouldAddFlex = false;
            var parts = value.Split('.');
            string actualValue = parts[0];
            string tailwindPrefix = "";

            if (parts.Length > 1)
            {
                tailwindPrefix = ConvertScreenSize(parts[1]);
            }

            // Specific replacements
            var classValue = actualValue
                .Replace("minmax", "_minmax")
                .Replace("1fr", "_1fr")
                .Replace(" ", "");

            // Handling specific grid column values such as "400px 400px"
            //if (actualValue.Contains(' '))
           // {
          //      var columns = actualValue.Split(' ');
          //      int columnCount = columns.Length;
          //      string columnSizes = string.Join(" ", columns);
          //      classValue = $"{columnCount} grid-template-columns-[{columnSizes}]";
         //   }

            return $"{tailwindPrefix}grid-cols-[{classValue}]";
        }

        private static string ConvertFxLayoutAlign(string value)
        {
            var parts = value.Split(' ').Select(part => part.Trim()).ToList();

            string mainAxis = parts.Count >= 1 ? parts[0] : "";
            string crossAxis = parts.Count == 2 ? parts[1] : "";

            var mainAxisMapping = new Dictionary<string, string>
            {
                {"start", "justify-start"},
                {"center", "justify-center"},
                {"end", "justify-end"},
                {"space-between", "justify-between"},
                {"space-around", "justify-around"}
            };

            var crossAxisMapping = new Dictionary<string, string>
            {
                {"start", "items-start"},
                {"center", "items-center"},
                {"end", "items-end"},
                {"baseline", "items-baseline"},
                {"stretch", "items-stretch"}
            };

            var mainClass = mainAxisMapping.ContainsKey(mainAxis) ? mainAxisMapping[mainAxis] : "";
            var crossClass = crossAxisMapping.ContainsKey(crossAxis) ? crossAxisMapping[crossAxis] : "";

            return $"{mainClass} {crossClass}".Trim();
        }

        private static string ConvertFxFlexOrder(string combinedString)
        {
            var parts = combinedString.Split(new string[] { "|||" }, StringSplitOptions.None);
            var attributeName = parts[0];
            var value = parts[1];

            var prefixParts = attributeName.Split('.');
            var prefixPart = prefixParts.Length > 1 ? prefixParts[1] : "";
            var tailwindPrefix = ConvertScreenSize(prefixPart);

            if (prefixPart.StartsWith("lt"))
            {
                var nextPrefix = GetNextScreenSize(prefixPart);
                var order = $"order-{value} {nextPrefix}order-{nextOrderValue}";
                nextOrderValue = Int32.Parse(value);
                return order;
            }

            return $"{tailwindPrefix}order-{value}";
        }


        private static string GetNextScreenSize(string prefixSize)
        {
            // Note: Add additional screen sizes if needed
            var order = new List<string> { "xs", "sm", "md", "lg", "xl" };
            for (int i = 0; i < order.Count - 1; i++)
            {
                if (prefixSize.EndsWith(order[i]))
                    return ConvertScreenSize(order[i]);
            }
            return "";
        }

       /* private static string ConvertFxFlexAlign(string value)
        {
            shouldAddFlex = false;
            value = value.Trim();

            var mapping = new Dictionary<string, string>
    {
        {"start", "align-start"},
        {"center", "align-center"},
        {"end", "align-end"},
        {"baseline", "align-baseline"},
        {"stretch", "align-stretch"}
    };

            return mapping.ContainsKey(value) ? mapping[value] : "";
        }
*/
    }
}
