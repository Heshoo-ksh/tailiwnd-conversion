using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tailiwnd_conversion
{
    internal class Program
    {
        private static string inputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-search.component.html";
        private static string outputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-search-processed.component.html";

        private static // Use a dictionary with lambda functions for conversion
            Dictionary<string, Func<string, string>> 
            conversionDictionary = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"fxLayout=\"row\"", _ => "flex"},
                {"fxLayout=\"column\"", _ => "flex-col"},

                {"fxLayoutGap", value => $"gap-[{value}]"},

                {"fxFlexFill", _ => "fill"},
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
                            mainAxis = parts[0];
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
                {"fxFlex.gt-xs", value => $"flex sm:basis-[{value}%]"},
                {"fxFlex.gt-sm", value => $"flex md:basis-[{value}%]"},
                {"fxFlex.gt-md", value => $"flex lg:basis-[{value}%]"},
                {"fxFlex.gt-lg", value => $"flex xl:basis-[{value}%]"},
                
                //---------fxFlex.lt-size-----
                {"fxFlex.lt-xs", value => $"flex basis-[{value}%]"},// Handle with caution, as it might not always be a direct conversion
                {"fxFlex.lt-sm", value => $"flex basis-[{value}%]"},
                {"fxFlex.lt-md", value => $"flex sm:basis-[{value}%]"},
                {"fxFlex.lt-lg", value => $"flex md:basis-[{value}%]"},
                {"fxFlex.lt-xl", value => $"flex lg:basis-[{value}%]"},

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

                    if (conversionDictionary.TryGetValue(key, out var converterFunc))
                    {
                        var newClass = converterFunc(attribute.Value);
                        existingClasses.Add(newClass);
                        node.Attributes.Remove(attribute);
                        //Console.WriteLine($"Converted {key} to {newClass} for node {node.Name}");
                    }
                    // For attributes with dynamic values (like fxLayoutGap)
                    else if (conversionDictionary.TryGetValue(attribute.Name, out converterFunc))
                    {
                        var newClass = converterFunc(attribute.Value);
                        if (newClass == $"gap-[{attribute.Value}]" && !existingClasses.Contains("flex"))
                        {
                            existingClasses.Add("flex");
                        }
                        existingClasses.Add(newClass);
                        node.Attributes.Remove(attribute);
                        // Console.WriteLine($"Converted {key} to {newClass} for node {node.Name}");
                    }

                    node.SetAttributeValue("class", string.Join(" ", existingClasses.ToArray()));
                }
            }
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

        private static void ReorderClassesForTailwindConvention(HtmlDocument doc)
        {
            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (node.HasAttributes && node.Attributes["class"] != null)
                {
                    var classes = node.Attributes["class"].Value.Split(' ').ToList();

                    // Sort the classes so that 'flex' and 'flex-*' classes come first
                    classes.Sort((a, b) =>
                    {
                        if (a == "flex" && b != "flex") return -1;   // 'flex' should always be first
                        if (a != "flex" && b == "flex") return 1;
                    
                        return 0;   // No preference for non-flex classes
                    });

                    node.Attributes["class"].Value = string.Join(" ", classes);
                }
            }
        }

    }
}
