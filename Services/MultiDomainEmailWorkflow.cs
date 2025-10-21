using System.Text;
using DigitalWonderlab.MultiDomainEmail.Services;
using Umbraco.Forms.Core;
using Umbraco.Forms.Core.Attributes;
using Umbraco.Forms.Core.Enums;
using Umbraco.Forms.Core.Models;

namespace DigitalWonderlab.MultiDomainEmail.Workflows
{
    public class MultiDomainEmailWorkflow : WorkflowType
    {
#if FORMS_15PLUS
    // Forms v15+ (including v16) - New property editor aliases
    private const string VIEW_TEXT = "Umb.PropertyEditorUi.TextBox";
    private const string VIEW_TEXTAREA = "Umb.PropertyEditorUi.TextArea";
    private const string VIEW_CHECKBOX = "Umb.PropertyEditorUi.Toggle";
#else
        private const string VIEW_TEXT = "TextField";
        private const string VIEW_TEXTAREA = "TextArea";
        private const string VIEW_CHECKBOX = "Checkbox";
#endif

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

        [Setting("Email", Description = "Enter the email address to send to", View = VIEW_TEXT)]
        public string Email { get; set; } = string.Empty;

        [Setting("Subject", Description = "Enter the email subject", View = VIEW_TEXT)]
        public string Subject { get; set; } = string.Empty;

        [Setting("Show All Form Fields", Description = "Include all form fields in the email?", View = VIEW_CHECKBOX)]
        public string ShowAllFields { get; set; } = "true";

        [Setting("Custom Message",
            Description = "Add a custom message above the form fields (optional). Use {fieldAlias} for specific fields.",
            View = VIEW_TEXTAREA)]
        public string CustomMessage { get; set; } = string.Empty;

        [Setting("Send Copy To Submitter",
            Description = "Send a copy to the person who submitted the form?",
            View = VIEW_CHECKBOX)]
        public string SendCopyToSubmitter { get; set; } = "false";

        [Setting("Submitter Email Field",
            Description = "Alias of the field with the submitter's email (e.g., 'email')",
            View = VIEW_TEXT)]
        public string SubmitterEmailField { get; set; } = string.Empty;


        public override async Task<WorkflowExecutionStatus> ExecuteAsync(WorkflowExecutionContext context)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    var processedSubject = ReplaceTokens(Subject, context);
                    var htmlMessage = BuildEmailContent(context);
                    await _emailService.SendEmailAsync(Email, processedSubject, htmlMessage, null);
                }

                if (bool.TryParse(SendCopyToSubmitter, out bool sendCopy) && sendCopy)
                {
                    var submitterEmail = GetSubmitterEmail(context);
                    if (!string.IsNullOrWhiteSpace(submitterEmail))
                    {
                        var thankYouSubject = "Thank you for your submission";
                        var thankYouHtml = BuildThankYouEmail(context);
                        await _emailService.SendEmailAsync(submitterEmail, thankYouSubject, thankYouHtml, null);
                    }
                }

                return WorkflowExecutionStatus.Completed;
            }
            catch
            {
                return WorkflowExecutionStatus.Failed;
            }
        }

        public override List<Exception> ValidateSettings()
        {
            var exceptions = new List<Exception>();

            if (string.IsNullOrWhiteSpace(Email))
                exceptions.Add(new ArgumentException("Email address is required"));

            if (string.IsNullOrWhiteSpace(Subject))
                exceptions.Add(new ArgumentException("Email subject is required"));

            return exceptions;
        }

        private string? GetSubmitterEmail(WorkflowExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(SubmitterEmailField))
                return null;

            foreach (var field in context.Record.RecordFields)
            {
                var fieldKey = field.Key.ToString() ?? string.Empty;
                if (string.Equals(fieldKey, SubmitterEmailField, StringComparison.OrdinalIgnoreCase))
                    return field.Value?.ToString();
            }
            return null;
        }

        private string ReplaceTokens(string template, WorkflowExecutionContext context)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            var result = template;

            foreach (var field in GetFormFieldsWithValues(context))
            {
                var nameToken = "{" + field.Name + "}";
                result = result.Replace(nameToken, field.Value, StringComparison.OrdinalIgnoreCase);

                var noSpaceToken = "{" + field.Name.Replace(" ", "") + "}";
                result = result.Replace(noSpaceToken, field.Value, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var field in context.Record.RecordFields)
            {
                var fieldKey = field.Key.ToString() ?? string.Empty;
                var token = "{" + fieldKey + "}";
                var value = field.Value?.ToString() ?? string.Empty;
                result = result.Replace(token, value, StringComparison.OrdinalIgnoreCase);
            }

            result = result.Replace("{formName}", context.Form.Name);
            result = result.Replace("{submissionDate}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            return result;
        }

        private string BuildEmailContent(WorkflowExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Form Submission</title></head>");
            sb.AppendLine("<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            sb.AppendLine($"<h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>Form Submission: {context.Form.Name}</h2>");
            sb.AppendLine($"<p style='color: #7f8c8d; margin-bottom: 30px;'><strong>Submitted:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");

            if (!string.IsNullOrWhiteSpace(CustomMessage))
            {
                var processedMessage = ReplaceTokens(CustomMessage, context).Replace("\r", "").Replace("\n", "<br/>");
                sb.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 30px;'>");
                sb.AppendLine(processedMessage);
                sb.AppendLine("</div>");
            }

            if (bool.TryParse(ShowAllFields, out bool showFields) && showFields)
            {
                sb.AppendLine("<h3 style='color: #2c3e50; margin-bottom: 15px;'>Form Details:</h3>");
                sb.AppendLine("<table style='width: 100%; border-collapse: collapse; background-color: white; box-shadow: 0 2px 5px rgba(0,0,0,0.1);'>");

                foreach (var field in GetFormFieldsWithValues(context))
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
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private string BuildThankYouEmail(WorkflowExecutionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Thank You</title></head>");
            sb.AppendLine("<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            sb.AppendLine("<h2 style='color: #27ae60;'>Thank you for your submission!</h2>");
            sb.AppendLine("<p>We have received your message and will get back to you as soon as possible.</p>");

            sb.AppendLine("<h3 style='color: #2c3e50; margin-top: 30px;'>Your submission details:</h3>");
            sb.AppendLine("<table style='width: 100%; border-collapse: collapse; background-color: #f8f9fa; border-radius: 5px;'>");

            foreach (var field in GetFormFieldsWithValues(context))
            {
                if (string.IsNullOrWhiteSpace(field.Value)) continue;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style='font-weight: bold; padding: 8px; width: 30%;'>{field.Name}:</td>");
                sb.AppendLine($"<td style='padding: 8px;'>{field.Value}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("<p style='margin-top: 20px; color: #7f8c8d;'>Best regards,<br/>The Team</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private List<(string Name, string Value)> GetFormFieldsWithValues(WorkflowExecutionContext context)
        {
            var fields = new List<(string Name, string Value)>();

            try
            {
                foreach (var formField in context.Form.AllFields)
                {
                    var fieldName = !string.IsNullOrEmpty(formField.Caption) ? formField.Caption : formField.Alias;
                    string fieldValue = string.Empty;

                    try
                    {
                        var fieldId = formField.Id.ToString();
                        var recordField = context.Record.RecordFields.FirstOrDefault(rf => rf.Key.ToString() == fieldId);

                        if (recordField.Key != null && recordField.Value != null)
                        {
                            var valueObj = recordField.Value;
                            var valueType = valueObj.GetType();

                            var valuesProp = valueType.GetProperty("Values");
                            if (valuesProp?.GetValue(valueObj) is IEnumerable<object> anyValues)
                                fieldValue = string.Join(", ", anyValues.Where(v => v != null).Select(v => v!.ToString()));

                            if (string.IsNullOrEmpty(fieldValue))
                            {
                                var valueProp = valueType.GetProperty("Value");
                                var single = valueProp?.GetValue(valueObj);
                                fieldValue = single?.ToString() ?? valueObj.ToString();
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
                fields.Add(("Debug Info", $"Error in GetFormFieldsWithValues: {ex.Message}"));
                foreach (var field in context.Record.RecordFields)
                {
                    var fieldKey = field.Key.ToString() ?? string.Empty;
                    var fieldValue = field.Value?.ToString() ?? string.Empty;
                    var displayName = MakeFieldNameReadable(fieldKey);
                    fields.Add((displayName, fieldValue));
                }
            }

            return fields;
        }

        private static string MakeFieldNameReadable(string fieldKey)
        {
            if (string.IsNullOrEmpty(fieldKey)) return fieldKey;
            var result = string.Concat(fieldKey.Select((x, i) => i > 0 && char.IsUpper(x) ? " " + x : x.ToString()));
            return result.Length > 0 ? char.ToUpper(result[0]) + result[1..] : result;
        }
    }
}
