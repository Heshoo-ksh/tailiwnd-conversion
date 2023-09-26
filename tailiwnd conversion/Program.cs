using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;


namespace tailiwnd_conversion
{

    internal class Program
    {
        static void Main(string[] args)
            {
                Console.WriteLine("Hello, World!");

                var inputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-search.component.html";
                var outputFilePath = @"C:\Users\hisha\OneDrive\Desktop\tailwind project\vendor-search-processed.component.html";

                var html = File.ReadAllText(inputFilePath);

                var doc = new HtmlDocument();
                doc.OptionOutputOriginalCase = true;
                doc.OptionWriteEmptyNodes = false;
                doc.LoadHtml(html);

            // Use a dictionary with lambda functions for conversion
            var conversionDictionary = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"fxLayout=\"row\"", _ => "flex"},
                {"fxLayout=\"column\"", _ => "flex-col"},
                {"fxLayoutGap", value => $"gap-[{value}]"},
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
                {"fxFlex.gt-xs", value => $"sm:basis-[{value}%]"},
                {"fxFlex.gt-sm", value => $"md:basis-[{value}%]"},
                {"fxFlex.gt-md", value => $"lg:basis-[{value}%]"},
                {"fxFlex.gt-lg", value => $"xl:basis-[{value}%]"},
                
                //---------fxFlex.lt-size-----
                {"fxFlex.lt-xs", value => $"basis-[{value}%]"},// Handle with caution, as it might not always be a direct conversion
                {"fxFlex.lt-sm", value => $"basis-[{value}%]"},
                {"fxFlex.lt-md", value => $"sm:basis-[{value}%]"},
                {"fxFlex.lt-lg", value => $"md:basis-[{value}%]"},
                {"fxFlex.lt-xl", value => $"lg:basis-[{value}%]"},

            };


            foreach (var node in doc.DocumentNode.DescendantsAndSelf())
                {
                    foreach (var attribute in node.Attributes.ToList()) // Use ToList() since we'll modify the collection inside the loop
                    {
                        var existingClasses = new HashSet<string>(node.GetAttributeValue("class", "").Split(' '));
                        var key = $"{attribute.Name}=\"{attribute.Value}\"";
                       // Console.WriteLine($"Trying to lookup: {key}");

                        // Direct match with dictionary (for attributes like fxlayout=row)
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

                var output = doc.DocumentNode.OuterHtml;
                File.WriteAllText(outputFilePath, output);

                var outputContent = File.ReadAllText(outputFilePath);
                outputContent = outputContent.Replace("=\"\"", "");
                File.WriteAllText(outputFilePath, outputContent);

                Console.WriteLine($"HTML conversion completed! Output written to {outputFilePath}");
                Console.ReadLine();

        }
    }
}