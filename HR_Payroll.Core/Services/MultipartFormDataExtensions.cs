using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace HR_Payroll.Core.Services
{
    public static class MultipartFormDataExtensions
    {
        public static async Task AddPropertiesAsync(this MultipartFormDataContent content, object obj)
        {
            if (obj == null) return;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var value = prop.GetValue(obj);
                if (value == null) continue;

                // Case 1: Single file
                if (value is IFormFile formFile)
                {
                    var fileContent = new StreamContent(formFile.OpenReadStream());
                    if (!string.IsNullOrEmpty(formFile.ContentType))
                    {
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(formFile.ContentType);
                    }
                    content.Add(fileContent, prop.Name, formFile.FileName ?? "file");
                }
                // Case 2: List or array of files
                else if (value is IEnumerable<IFormFile> fileList && !(value is string))
                {
                    foreach (var file in fileList)
                    {
                        if (file == null) continue;

                        var fileContent = new StreamContent(file.OpenReadStream());
                        if (!string.IsNullOrEmpty(file.ContentType))
                        {
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                        }

                        // ✅ Use the same key (e.g., "Images") for all files
                        content.Add(fileContent, prop.Name, file.FileName ?? "file");
                    }
                }
                // Case 3: Complex objects (serialize to JSON)
                else if (IsComplexType(value.GetType()))
                {
                    var jsonValue = JsonSerializer.Serialize(value);
                    var stringContent = new StringContent(jsonValue, Encoding.UTF8, "application/json");
                    content.Add(stringContent, prop.Name);
                }
                // Case 4: Primitive types and strings
                else
                {
                    content.Add(new StringContent(value.ToString() ?? string.Empty), prop.Name);
                }
            }
        }

        private static bool IsComplexType(Type type)
        {
            return !type.IsPrimitive
                   && !type.IsEnum
                   && type != typeof(string)
                   && type != typeof(decimal)
                   && type != typeof(DateTime)
                   && type != typeof(DateTimeOffset)
                   && type != typeof(TimeSpan)
                   && type != typeof(Guid);
        }
    }
}
