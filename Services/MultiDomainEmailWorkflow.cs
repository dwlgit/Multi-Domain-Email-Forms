using System.Reflection;
using System.Text;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Attributes;
using Umbraco.Forms.Core.Enums;
using Umbraco.Forms.Core.Models;
using DigitalWonderlab.MultiDomainEmail.Services;

namespace DigitalWonderlab.MultiDomainEmail.Workflows
{
    public class MultiDomainEmailWorkflow : WorkflowType
    {
        private readonly IMultiDomainEmailService _emailService;

        public MultiDomainEmailWorkflow(IMultiDomainEmailService emailService)
        {
            _emailService = emailService;

            Id = new Guid("A1B2C3D4-E5F6-7890-ABCD-123456789012");
            Name = "Multi-Domain Email";
            Description = "Send email using domain-specific SMTP settings";
            Icon = "icon-mail";
            Group = "Services";
        }

        [Setting("Email",
            Description = "Enter the email address to send to",
            View = "TextField")]
        public string Email { get; set; } = "";

        [Setting("Subject",
            Description = "Enter the email subject",
            View = "TextField")]
        public string Subject { get; set; } = "";

        [Setting("Show All Form Fields",
            Description = "Include all form fields in the email?",
            View = "Checkbox")]
        public string ShowAllFields { get; set; } = "1";

        [Setting("Custom Message",
            Description = "Add a custom message above the form fields (optional). Use {fieldAlias} for specific fields.",
            View = "TextArea")]
        public string CustomMessage { get; set; } = "";

        [Setting("Send Copy To Submitter",
            Description = "Send a copy to the person who submitted the form?",
            View = "Checkbox")]
        public string SendCopyToSubmitter { get; set; } = "";

        [Setting("Submitter Email Field",
            Description = "Enter the alias of the field that contains the submitter's email address (e.g., 'email')",
            View = "TextField")]
        public string SubmitterEmailField { get; set; } = "";

        public override async Task<WorkflowExecutionStatus> ExecuteAsync(WorkflowExecutionContext context)
        {
            try
            {
                if (!string.IsNullOrEmpty(Email))
                {
                    var processedSubject = ReplaceTokens(Subject, context);
                    var htmlMessage = BuildEmailContent(context);

                    await _emailService.SendEmailAsync(Email, processedSubject, htmlMessage, null);
                }

                if (!string.IsNullOrEmpty(SendCopyToSubmitter) && SendCopyToSubmitter == "1")
                {
                    var submitterEmail = GetSubmitterEmail(context);
                    if (!string.IsNullOrEmpty(submitterEmail))
                    {
                        var thankYouSubject = "Thank you for your submission";
                        var thankYouHtml = BuildThankYouEmail(context);

                        await _emailService.SendEmailAsync(submitterEmail, thankYouSubject, thankYouHtml, null);
                    }
                }

                return WorkflowExecutionStatus.Completed;
            }
            catch (Exception ex)
            {
                return WorkflowExecutionStatus.Failed;
            }
        }

        public override List<Exception> ValidateSettings()
        {
            var exceptions = new List<Exception>();

            if (string.IsNullOrEmpty(Email))
            {
                exceptions.Add(new ArgumentException("Email address is required"));
            }

            if (string.IsNullOrEmpty(Subject))
            {
                exceptions.Add(new ArgumentException("Email subject is required"));
            }

            return exceptions;
        }

        private string GetSubmitterEmail(WorkflowExecutionContext context)
        {
            if (string.IsNullOrEmpty(SubmitterEmailField))
                return null;

            foreach (var field in context.Record.RecordFields)
            {
                var fieldKey = field.Key.ToString() ?? "";
                if (string.Equals(fieldKey, SubmitterEmailField, StringComparison.OrdinalIgnoreCase))
                {
                    return field.Value?.ToString();
                }
            }

            return null;
        }

        private string ReplaceTokens(string template, WorkflowExecutionContext context)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            var result = template;

            var formFields = GetFormFieldsWithValues(context);

            foreach (var field in formFields)
            {
                var nameToken = "{" + field.Name + "}";
                result = result.Replace(nameToken, field.Value, StringComparison.OrdinalIgnoreCase);

                var noSpaceToken = "{" + field.Name.Replace(" ", "") + "}";
                result = result.Replace(noSpaceToken, field.Value, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var field in context.Record.RecordFields)
            {
                var fieldKey = field.Key.ToString() ?? "";
                var token = "{" + fieldKey + "}";
                var value = field.Value?.ToString() ?? "";
                result = result.Replace(token, value, StringComparison.OrdinalIgnoreCase);
            }

            result = result.Replace("{formName}", context.Form.Name);
            result = result.Replace("{submissionDate}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            return result;
        }

        private string BuildEmailContent(WorkflowExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<title>Form Submission</title>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            sb.AppendLine($"<h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>Form Submission: {context.Form.Name}</h2>");
            sb.AppendLine($"<p style='color: #7f8c8d; margin-bottom: 30px;'><strong>Submitted:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");

            if (!string.IsNullOrEmpty(CustomMessage))
            {
                var processedMessage = ReplaceTokens(CustomMessage, context);
                var htmlMessage = processedMessage.Replace("\n", "<br/>").Replace("\r", "");
                sb.AppendLine($"<div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 30px;'>");
                sb.AppendLine($"{htmlMessage}");
                sb.AppendLine("</div>");
            }

            if (string.IsNullOrEmpty(ShowAllFields) || ShowAllFields == "1")
            {
                sb.AppendLine("<h3 style='color: #2c3e50; margin-bottom: 15px;'>Form Details:</h3>");
                sb.AppendLine("<table style='width: 100%; border-collapse: collapse; background-color: white; box-shadow: 0 2px 5px rgba(0,0,0,0.1);'>");

                var formFields = GetFormFieldsWithValues(context);

                foreach (var field in formFields)
                {
                    if (string.IsNullOrWhiteSpace(field.Value)) continue;

                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td style='background-color: #f8f9fa; font-weight: bold; padding: 12px; border: 1px solid #dee2e6; width: 30%;'>{field.Name}</td>");
                    sb.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{field.Value}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</table>");
            }

            sb.AppendLine("<hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'/>");
            sb.AppendLine("<p style='color: #7f8c8d; font-size: 12px; text-align: center;'>This email was automatically generated from your website contact form.</p>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string BuildThankYouEmail(WorkflowExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<title>Thank You</title>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            sb.AppendLine("<h2 style='color: #27ae60;'>Thank you for your submission!</h2>");
            sb.AppendLine("<p>We have received your message and will get back to you as soon as possible.</p>");

            sb.AppendLine("<h3 style='color: #2c3e50; margin-top: 30px;'>Your submission details:</h3>");
            sb.AppendLine("<table style='width: 100%; border-collapse: collapse; background-color: #f8f9fa; border-radius: 5px;'>");

            var formFields = GetFormFieldsWithValues(context);

            foreach (var field in formFields)
            {
                if (string.IsNullOrWhiteSpace(field.Value)) continue;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style='font-weight: bold; padding: 8px; width: 30%;'>{field.Name}:</td>");
                sb.AppendLine($"<td style='padding: 8px;'>{field.Value}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p style='margin-top: 20px; color: #7f8c8d;'>Best regards,<br/>The Team</p>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private List<(string Name, string Value)> GetFormFieldsWithValues(WorkflowExecutionContext context)
        {
            var fields = new List<(string Name, string Value)>();

            try
            {
                // Method 1: Try accessing via GetRecordFieldValue method if available
                foreach (var formField in context.Form.AllFields)
                {
                    var fieldName = !string.IsNullOrEmpty(formField.Caption) ? formField.Caption : formField.Alias;
                    string fieldValue = "";

                    // Try to get value using Umbraco's method
                    try
                    {
                        var recordService = context.Record;
                        var fieldId = formField.Id.ToString();

                        // Look for the field in record fields and extract value properly
                        var recordField = context.Record.RecordFields
                            .Where(rf => rf.Key.ToString() == fieldId)
                            .FirstOrDefault();

                        if (recordField.Key != null && recordField.Value != null)
                        {
                            // Try to access the Values property using reflection
                            var valueObj = recordField.Value;
                            var valueType = valueObj.GetType();

                            // Look for Values property
                            var valuesProperty = valueType.GetProperty("Values");
                            if (valuesProperty != null)
                            {
                                var valuesCollection = valuesProperty.GetValue(valueObj);
                                if (valuesCollection != null)
                                {
                                    if (valuesCollection is IEnumerable<object> enumerable)
                                    {
                                        fieldValue = string.Join(", ", enumerable.Where(v => v != null).Select(v => v.ToString()));
                                    }
                                    else if (valuesCollection is IEnumerable<string> stringEnumerable)
                                    {
                                        fieldValue = string.Join(", ", stringEnumerable);
                                    }
                                    else
                                    {
                                        fieldValue = valuesCollection.ToString();
                                    }
                                }
                            }

                            // If Values property didn't work, try other common properties
                            if (string.IsNullOrEmpty(fieldValue))
                            {
                                var valueProperty = valueType.GetProperty("Value");
                                if (valueProperty != null)
                                {
                                    var value = valueProperty.GetValue(valueObj);
                                    fieldValue = value?.ToString() ?? "";
                                }
                                else
                                {
                                    fieldValue = valueObj.ToString();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        fieldValue = $"Error getting value: {ex.Message}";
                    }

                    fields.Add((fieldName, fieldValue));
                }
            }
            catch (Exception ex)
            {
                // Complete fallback - add error info for debugging
                fields.Add(("Debug Info", $"Error in GetFormFieldsWithValues: {ex.Message}"));

                // Try the simple approach
                foreach (var field in context.Record.RecordFields)
                {
                    var fieldKey = field.Key.ToString() ?? "";
                    var fieldValue = field.Value?.ToString() ?? "";
                    var displayName = MakeFieldNameReadable(fieldKey);

                    fields.Add((displayName, fieldValue));
                }
            }

            return fields;
        }

        private string MakeFieldNameReadable(string fieldKey)
        {
            if (string.IsNullOrEmpty(fieldKey)) return fieldKey;

            // Convert camelCase or PascalCase to readable format
            var result = string.Concat(fieldKey.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));

            // Capitalize first letter
            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1);
            }

            return result;
        }
    }
}