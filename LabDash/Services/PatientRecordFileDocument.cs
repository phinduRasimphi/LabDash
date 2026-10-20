using LabDash.Helpers;
using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabDash.Services
{
    public class PatientRecordFileDocument : IDocument
    {
        private const string ColInk = "#1B2420";
        private const string ColMuted = "#5C6560";
        private const string ColLine = "#C9D1CD";
        private const string ColAccent = "#0A5850";
        private const string ColHeadFill = "#EEF2F0";

        private readonly string _patientName;
        private readonly string _idNumber;
        private readonly string _recordType; // "Condition", "Allergy", or "Medication"
        private readonly object _recordModel;
        private readonly DateTime _generated;
        private readonly byte[]? _logo;

        public PatientRecordFileDocument(
            string patientName,
            string idNumber,
            string recordType,
            object recordModel,
            DateTime generated,
            byte[]? logo = null)
        {
            _patientName = patientName;
            _idNumber = idNumber;
            _recordType = recordType;
            _recordModel = recordModel;
            _generated = generated;
            _logo = logo;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = $"{_recordType} Record File", Author = "LabDash" };
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(36);
                page.MarginHorizontal(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(ColInk));

                page.Header().SkipOnce().Element(RunningHeader);
                page.Content().Element(Body);
                page.Footer().Element(FooterBar);
            });
        }

        private void RunningHeader(IContainer c)
        {
            c.PaddingBottom(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"{_recordType} Record File").Bold().FontSize(9);
                    row.AutoItem().Text($"{_patientName} | ID: {_idNumber}").FontSize(9).FontColor(ColMuted);
                });
                col.Item().PaddingTop(4).LineHorizontal(0.75f).LineColor(ColLine);
            });
        }

        private void FooterBar(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(ColLine);
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("LabDash | Patient Portal").FontSize(8).FontColor(ColMuted);
                    row.RelativeItem().AlignCenter().Text($"Generated {_generated:d MMMM yyyy}").FontSize(8).FontColor(ColMuted);
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(ColMuted));
                        t.Span("Page "); t.CurrentPageNumber(); t.Span(" of "); t.TotalPages();
                    });
                });
            });
        }

        private void Body(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(14);

                // Title Block
                col.Item().Column(titleCol =>
                {
                    if (_logo != null)
                        titleCol.Item().AlignLeft().Height(34).Image(_logo).FitHeight();
                    else
                        titleCol.Item().Text("LabDash").FontSize(9).Bold().FontColor(ColAccent);

                    titleCol.Item().PaddingTop(6).Text($"{_recordType} Details File").FontSize(20).Bold();
                    titleCol.Item().PaddingTop(2).Text($"Individual health record entry for {_patientName}").FontColor(ColMuted);
                    titleCol.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(ColAccent);
                });

                // Details Grid
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(sub =>
                    {
                        KeyValue(sub, "Patient", _patientName);
                        KeyValue(sub, "ID number", _idNumber);
                    });
                    row.RelativeItem().Column(sub =>
                    {
                        KeyValue(sub, "Generated", $"{_generated:d MMMM yyyy} at {_generated:HH:mm}");
                    });
                });

                col.Item().Element(x => SectionHeading(x, $"{_recordType} Specification"));

                // Dynamic Property Mapping based on Record Type
                col.Item().Background(ColHeadFill).Border(0.75f).BorderColor(ColLine).Padding(12).Column(details =>
                {
                    details.Spacing(6);
                    if (_recordModel is ConditionRecordViewModel cond)
                    {
                        DetailRow(details, "Condition Name", cond.Name);
                        DetailRow(details, "Category", cond.CategoryName ?? "General");
                        DetailRow(details, "Diagnosis Date", cond.DiagnosisDate?.ToString("dd MMMM yyyy") ?? "Not specified");
                        DetailRow(details, "Severity", cond.Severity ?? "Not specified");
                        DetailRow(details, "Recorded By", cond.RecordedByDoctorName ?? "Patient Profile Entry");
                        DetailRow(details, "Clinical Notes", string.IsNullOrWhiteSpace(cond.Notes) ? "None" : cond.Notes);
                    }
                    else if (_recordModel is AllergyRecordViewModel allergy)
                    {
                        DetailRow(details, "Allergy Name", allergy.Name);
                        DetailRow(details, "Category", allergy.CategoryName ?? "General");
                        DetailRow(details, "Recorded Date", allergy.RecordedDate?.ToString("dd MMMM yyyy") ?? "Not specified");
                        DetailRow(details, "Severity", allergy.Severity ?? "Not specified");
                        DetailRow(details, "Recorded By", allergy.RecordedByDoctorName ?? "Patient Profile Entry");
                        DetailRow(details, "Clinical Notes", string.IsNullOrWhiteSpace(allergy.Notes) ? "None" : allergy.Notes);
                    }
                    else if (_recordModel is MedicationRecordViewModel med)
                    {
                        DetailRow(details, "Medication Name", med.Name);
                        DetailRow(details, "Category", med.CategoryName ?? "General");
                        DetailRow(details, "Dosage", med.Dosage ?? "Not specified");
                        DetailRow(details, "Frequency", med.Frequency ?? "Not specified");
                        DetailRow(details, "Start Date", med.StartDate?.ToString("dd MMMM yyyy") ?? "Not specified");
                        DetailRow(details, "End Date", med.EndDate?.ToString("dd MMMM yyyy") ?? "Ongoing");
                        DetailRow(details, "Recorded By", med.RecordedByDoctorName ?? "Patient Profile Entry");
                        DetailRow(details, "Clinical Notes", string.IsNullOrWhiteSpace(med.Notes) ? "None" : med.Notes);
                    }
                });
            });
        }

        private static void KeyValue(ColumnDescriptor col, string label, string value)
        {
            col.Item().PaddingBottom(2).Row(row =>
            {
                row.ConstantItem(74).Text(label).FontColor(ColMuted);
                row.RelativeItem().Text(value).Bold();
            });
        }

        private static void DetailRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(120).Text(label).Bold().FontColor(ColMuted);
                row.RelativeItem().Text(value);
            });
        }

        private static void SectionHeading(IContainer c, string text)
        {
            c.BorderBottom(0.75f).BorderColor(ColLine).PaddingBottom(3)
                .Text(text).FontSize(12).Bold().FontColor(ColAccent);
        }
    }
}