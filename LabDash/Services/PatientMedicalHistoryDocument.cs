using LabDash.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace LabDash.Services
{
    public class PatientMedicalHistoryDocument : IDocument
    {
        private const string ColInk = "#1B2420";
        private const string ColMuted = "#5C6560";
        private const string ColLine = "#C9D1CD";
        private const string ColAccent = "#0A5850";
        private const string ColHeadFill = "#EEF2F0";

        private readonly string _patientName;
        private readonly string _idNumber;
        private readonly MedicalHistoryViewModel _model;
        private readonly DateTime _generated;
        private readonly byte[]? _logo;

        public PatientMedicalHistoryDocument(
            string patientName,
            string idNumber,
            MedicalHistoryViewModel model,
            DateTime generated,
            byte[]? logo = null)
        {
            _patientName = patientName;
            _idNumber = idNumber;
            _model = model;
            _generated = generated;
            _logo = logo;
        }

        public DocumentMetadata GetMetadata() => new DocumentMetadata { Title = "Medical History Summary", Author = "LabDash" };
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
                    row.RelativeItem().Text("Medical History Summary").Bold().FontSize(9);
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

                    titleCol.Item().PaddingTop(6).Text("Medical History Summary").FontSize(20).Bold();
                    titleCol.Item().PaddingTop(2).Text($"Patient Record & Health Profile for {_patientName}").FontColor(ColMuted);
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

                // 1. Conditions Section
                col.Item().Element(x => SectionHeading(x, "Medical Conditions"));
                if (_model.Conditions == null || !_model.Conditions.Any())
                {
                    col.Item().Text("No medical conditions recorded.").Italic().FontColor(ColMuted);
                }
                else
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.ConstantColumn(70); cd.RelativeColumn(2); });
                        table.Header(h =>
                        {
                            h.Cell().Element(HeadCell).Text("Condition").Bold();
                            h.Cell().Element(HeadCell).Text("Category").Bold();
                            h.Cell().Element(HeadCell).Text("Diagnosed").Bold();
                            h.Cell().Element(HeadCell).Text("Severity").Bold();
                        });
                        foreach (var item in _model.Conditions)
                        {
                            table.Cell().Element(BodyCell).Text(item.Name);
                            table.Cell().Element(BodyCell).Text(item.CategoryName ?? "-");
                            table.Cell().Element(BodyCell).Text(item.DiagnosisDate.HasValue ? item.DiagnosisDate.Value.ToString("dd MMM yyyy") : "-");
                            table.Cell().Element(BodyCell).Text(item.Severity ?? "-");
                        }
                    });
                }

                // 2. Allergies Section
                col.Item().Element(x => SectionHeading(x, "Allergies"));
                if (_model.Allergies == null || !_model.Allergies.Any())
                {
                    col.Item().Text("No allergies recorded.").Italic().FontColor(ColMuted);
                }
                else
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.ConstantColumn(70); cd.RelativeColumn(2); });
                        table.Header(h =>
                        {
                            h.Cell().Element(HeadCell).Text("Allergy").Bold();
                            h.Cell().Element(HeadCell).Text("Category").Bold();
                            h.Cell().Element(HeadCell).Text("Recorded").Bold();
                            h.Cell().Element(HeadCell).Text("Severity").Bold();
                        });
                        foreach (var item in _model.Allergies)
                        {
                            table.Cell().Element(BodyCell).Text(item.Name);
                            table.Cell().Element(BodyCell).Text(item.CategoryName ?? "-");
                            // Fixed from DiagnosisDate to RecordedDate for AllergyRecordViewModel
                            table.Cell().Element(BodyCell).Text(item.RecordedDate.HasValue ? item.RecordedDate.Value.ToString("dd MMM yyyy") : "-");
                            table.Cell().Element(BodyCell).Text(item.Severity ?? "-");
                        }
                    });
                }

                // 3. Medications Section
                col.Item().Element(x => SectionHeading(x, "Medications"));
                if (_model.Medications == null || !_model.Medications.Any())
                {
                    col.Item().Text("No medications recorded.").Italic().FontColor(ColMuted);
                }
                else
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(2); cd.RelativeColumn(2); cd.ConstantColumn(70); });
                        table.Header(h =>
                        {
                            h.Cell().Element(HeadCell).Text("Medication").Bold();
                            h.Cell().Element(HeadCell).Text("Dosage").Bold();
                            h.Cell().Element(HeadCell).Text("Frequency").Bold();
                            h.Cell().Element(HeadCell).Text("Started").Bold();
                        });
                        foreach (var item in _model.Medications)
                        {
                            table.Cell().Element(BodyCell).Text(item.Name);
                            table.Cell().Element(BodyCell).Text(item.Dosage ?? "-");
                            table.Cell().Element(BodyCell).Text(item.Frequency ?? "-");
                            table.Cell().Element(BodyCell).Text(item.StartDate.HasValue ? item.StartDate.Value.ToString("dd MMM yyyy") : "-");
                        }
                    });
                }
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

        private static void SectionHeading(IContainer c, string text)
        {
            c.BorderBottom(0.75f).BorderColor(ColLine).PaddingBottom(3)
                .Text(text).FontSize(12).Bold().FontColor(ColAccent);
        }

        private static IContainer HeadCell(IContainer c) =>
            c.Background(ColHeadFill).BorderBottom(1).BorderColor(ColMuted).PaddingVertical(4).PaddingHorizontal(5);

        private static IContainer BodyCell(IContainer c) =>
            c.Background("#FFFFFF").BorderBottom(0.5f).BorderColor(ColLine).PaddingVertical(4).PaddingHorizontal(5);
    }
}