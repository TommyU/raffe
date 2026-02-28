using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using Raffe.Models;

namespace Raffe.Services;

public class ExcelImportService
{
    public (List<Participant> participants, List<string> errors) Import(string filePath)
    {
        var participants = new List<Participant>();
        var errors = new List<string>();

        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            var rowNum = 2;
            foreach (var row in rows)
            {
                var name = row.Cell(1).GetString().Trim();
                var department = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"第{rowNum}行：姓名为空，已跳过");
                    rowNum++;
                    continue;
                }

                participants.Add(new Participant
                {
                    Name = name,
                    Department = department
                });
                rowNum++;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"导入失败：{ex.Message}");
        }

        return (participants, errors);
    }

    public void ExportTemplate(string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("参与者");
        worksheet.Cell(1, 1).Value = "姓名";
        worksheet.Cell(1, 2).Value = "部门";
        worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
        worksheet.Column(1).Width = 15;
        worksheet.Column(2).Width = 20;
        workbook.SaveAs(filePath);
    }
}
