namespace DigitalWonderlab.MultiDomainEmail.Models
{
    public class EmailTemplateModel
    {
        public string FormName { get; set; } = string.Empty;
        public string SubmissionDate { get; set; } = string.Empty;
        public string CustomMessage { get; set; } = string.Empty;
        public bool ShowAllFields { get; set; }
        public List<FormField> Fields { get; set; } = new();
    }

    public class FormField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}