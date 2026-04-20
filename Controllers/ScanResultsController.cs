using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Syngenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanResultsController : ControllerBase
{
    private readonly string _conn =
        "Host=db.viunjccoqeefdeakqqwg.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=Projectsyngenta23.;SSL Mode=Require;Trust Server Certificate=true;";

    // ✅ POST (FIX TOTAL - ANTI ERROR 500)
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] dynamic data)
    {
        try
        {
            // ✅ amanin timestamp
            DateTime time = DateTime.Now;
            if (data.timestamp != null)
            {
                DateTime.TryParse(data.timestamp.ToString(), out time);
            }

            // ✅ amanin semua field
            string camera = data.cameraId != null ? data.cameraId.ToString() : "";
            string qr = data.qrCode != null ? data.qrCode.ToString() : null;
            string status = data.status != null ? data.status.ToString() : "";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO scan_results (scan_time, camera_id, qr_code, status)
                VALUES (@time, @camera, @qr, @status)";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@time", time);
            cmd.Parameters.AddWithValue("@camera", camera);
            cmd.Parameters.AddWithValue("@qr", (object?)qr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", status);

            await cmd.ExecuteNonQueryAsync();

            return Ok("Saved");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ✅ GET DATA
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var list = new List<object>();

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = "SELECT id, scan_time, camera_id, qr_code, status FROM scan_results ORDER BY id DESC";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader.GetInt32(0),
                    time = reader.GetDateTime(1),
                    camera = reader.GetString(2),
                    qr = reader.IsDBNull(3) ? null : reader.GetString(3),
                    status = reader.GetString(4)
                });
            }

            return Ok(list);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ✅ EXPORT CSV
    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        try
        {
            var list = new List<string>();

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = "SELECT id, scan_time, camera_id, qr_code, status FROM scan_results ORDER BY id DESC";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            list.Add("Id,Time,Camera,QR,Status");

            while (await reader.ReadAsync())
            {
                var line = $"{reader.GetInt32(0)},{reader.GetDateTime(1)},{reader.GetString(2)},{(reader.IsDBNull(3) ? "" : reader.GetString(3))},{reader.GetString(4)}";
                list.Add(line);
            }

            var csv = string.Join("\n", list);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", "scan_results.csv");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ✅ EXPORT EXCEL
    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ScanResults");

            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Time";
            ws.Cell(1, 3).Value = "Camera";
            ws.Cell(1, 4).Value = "QR";
            ws.Cell(1, 5).Value = "Status";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = "SELECT id, scan_time, camera_id, qr_code, status FROM scan_results";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            int row = 2;

            while (await reader.ReadAsync())
            {
                ws.Cell(row, 1).Value = reader.GetInt32(0);
                ws.Cell(row, 2).Value = reader.GetDateTime(1);
                ws.Cell(row, 3).Value = reader.GetString(2);
                ws.Cell(row, 4).Value = reader.IsDBNull(3) ? "" : reader.GetString(3);
                ws.Cell(row, 5).Value = reader.GetString(4);
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "scan_results.xlsx"
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}