using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace tailiwnd_conversion
{
    internal class Program
    {
        private static string inputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-edit.component.html";
        private static string outputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-edit-processed.component.html";
        private static bool shouldAddFlex = true; // The flag that controls whether "flex" should be added

        private static // Use a dictionary with lambda functions for conversion
            Dictionary<string, Func<string, string>> 
            conversionDictionary = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"fxLayout=\"row\"", _ => "flex"},
                {"fxLayout=\"column\"", _ => "flex-col"},

                {"fxLayoutGap", ConvertFxLayoutGap},

                {"fxFlexFill", _ => {shouldAddFlex = false; return "fill"; } },
                //---------fxLayoutAlign-----------
                {"fxLayoutAlign", value =>
                    {
                        var parts = value.Split(' ').Select(part => part.Trim()).ToList();

                        string mainAxis = "";
                        string crossAxis = "";

                        if (parts.Count == 1)
                        {
                            mainAxis = parts[0];
                        }
                        else if (parts.Count == 2)
                        {
                            mainAxis += parts[0];
                            crossAxis = parts[1];
                        }
                        else
                        {
                            return ""; // return empty string if the value format is not recognized
                        }

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
                },
                
                //---------fxFlex.gt-size-----
                {"fxFlex.gt-xs", value => ConvertFxFlex(value, "xs", "gt")},
                {"fxFlex.gt-sm", value => ConvertFxFlex(value, "sm", "gt")},
                {"fxFlex.gt-md", value => ConvertFxFlex(value, "md", "gt")},
                {"fxFlex.gt-lg", value => ConvertFxFlex(value, "lg", "gt")},

                //---------fxFlex.lt-size-----
                {"fxFlex.lt-xs", value => ConvertFxFlex(value, "xs", "lt")},
                {"fxFlex.lt-sm", value => ConvertFxFlex(value, "sm", "lt")},
                {"fxFlex.lt-md", value => ConvertFxFlex(value, "md", "lt")},
                {"fxFlex.lt-lg", value => ConvertFxFlex(value, "lg", "lt")},
                {"fxFlex.lt-xl", value => ConvertFxFlex(value, "xl", "lt")},

                //----grid layout with resize
                {"gdColumns.xs", value => ConvertGdColumns("xs", value)},
                {"gdColumns.sm", value => ConvertGdColumns("sm", value)},
                {"gdColumns.md", value => ConvertGdColumns("md", value)},
                {"gdColumns.lg", value => ConvertGdColumns("lg", value)},
                {"gdColumns.xl", value => ConvertGdColumns("xl", value)},
                {"gdColumns.gt-xs", value => ConvertGdColumns("gt-xs", value)},
                {"gdColumns.gt-sm", value => ConvertGdColumns("gt-sm", value)},
                {"gdColumns.gt-md", value => ConvertGdColumns("gt-md", value)},
                {"gdColumns.gt-lg", value => ConvertGdColumns("gt-lg", value)},
                {"gdColumns.lt-xs", value => ConvertGdColumns("lt-xs", value)},
                {"gdColumns.lt-sm", value => ConvertGdColumns("lt-sm", value)},
                {"gdColumns.lt-md", value => ConvertGdColumns("lt-md", value)},
                {"gdColumns.lt-lg", value => ConvertGdColumns("lt-lg", value)},
                //----grid layout without resize


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
                foreach (var attribute in node.Attributes.ToList()) // Use ToList() to avoid collection modification errors
                {
                    var existingClasses = new HashSet<string>(node.GetAttributeValue("class", "").Split(' '));
                    var key = $"{attribute.Name}=\"{attribute.Value}\"";

                    shouldAddFlex = true; // Reset before each conversion

                    if (conversionDictionary.TryGetValue(key, out var converterFunc) || conversionDictionary.TryGetValue(attribute.Name, out converterFunc))
                    {
                        var newClass = converterFunc(attribute.Value);
                        existingClasses.Add(newClass);
                        node.Attributes.Remove(attribute);

                        // Ensure grid-flow-row and correct order
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
            // Placeholders for checking existence
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

            // Ensure 'flex' comes first
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

            // 'grid-cols' should come last
            if (isAGridCols) return 1;
            if (isBGridCols) return -1;

            return 0; // In case none of the above conditions match, which should ideally not happen
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

        private static string ConvertFxLayoutGap(string value)
        {
            // Split the value into parts
            var parts = value.Split(' ').Select(part => part.Trim()).ToList();

            // Determine layout type: flex (default)
            var layoutType = "flex";
            if (parts.Contains("grid"))
            {
                layoutType = "grid";
                parts.Remove("grid");
                shouldAddFlex = false;  // set flag to false so flex is not added
            }

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

            return $"{layoutType} {gapX} {gapY}".Trim();
        }

        private static string ConvertFxFlex(string value, string screenSize, string prefix)
        {
            string tailwindPrefix = ConvertScreenSize(screenSize, prefix);
            // Here we are assuming that we're just setting the basis based on the value.
            return $"{tailwindPrefix}basis-[{value}%]";
        }

        private static string ConvertScreenSize(string size, string prefix)
        {
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

        private static string ConvertGdColumns(string sizePrefix, string value)
        {
            // Splitting the prefix into gt/lt and the actual size (e.g., "gt-xs" => "gt", "xs")
            var prefixParts = sizePrefix.Split('-');
            var prefix = prefixParts.Length > 1 ? prefixParts[0] : "";
            var size = prefixParts.Length > 1 ? prefixParts[1] : prefixParts[0];

            var tailwindPrefix = ConvertScreenSize(size, prefix);

            // Specific replacements
            var classValue = value
                .Replace("minmax", "_minmax")
                .Replace("1fr", "_1fr")
                .Replace(" ", "");

            return $"{tailwindPrefix}grid-cols-[{classValue}]";
        }

        private static void EnsureGridFlowRowAndOrder(HashSet<string> existingClasses)
        {
            // Ensure grid-flow-row is present if grid-cols-* is present
            if (existingClasses.Any(c => c.StartsWith("grid-cols-")) && !existingClasses.Contains("grid-flow-row"))
            {
                existingClasses.Add("grid-flow-row");
            }
        }




    }
}
