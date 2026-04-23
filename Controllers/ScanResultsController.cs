


using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ClosedXML.Excel;
using Syngenta.API.Models;

namespace Syngenta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanResultsController : ControllerBase
{
    private readonly string _conn =
        "Host=aws-1-ap-northeast-2.pooler.supabase.com;Database=postgres;Username=postgres.viunjccoqeefdeakqqwg;Password=Projectsyngenta23.;SSL Mode=Require;Trust Server Certificate=true";

    // ================================
    // ✅ POST SCAN RESULT
    // ================================
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ScanResultDto data)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO scan_results (scan_time, camera_id, qr_code, status)
                VALUES (@time, @camera, @qr, @status)";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@time", data.timestamp);
            cmd.Parameters.AddWithValue("@camera", data.cameraId ?? "");
            cmd.Parameters.AddWithValue("@qr", (object?)data.qrCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", data.status ?? "");

            await cmd.ExecuteNonQueryAsync();

            return Ok("Saved");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }

    // ================================
    // 🔥 DELETE ALL HISTORY (INI YANG BARU)
    // ================================
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAll()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("DELETE FROM scan_results", conn);
            await cmd.ExecuteNonQueryAsync();

            return Ok("Semua data berhasil dihapus");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("upload-txt")]
public async Task<IActionResult> UploadTxt(IFormFile file)
{
    try
    {
        if (file == null || file.Length == 0)
            return BadRequest("File kosong");

        using var reader = new StreamReader(file.OpenReadStream());

        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();

        // hapus data lama
        var clear = new NpgsqlCommand("DELETE FROM qr_references", conn);
        await clear.ExecuteNonQueryAsync();

        int seq = 1;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cmd = new NpgsqlCommand(
                "INSERT INTO qr_references (qr_code, sequence) VALUES (@qr, @seq)",
                conn
            );

            cmd.Parameters.AddWithValue("@qr", line.Trim());
            cmd.Parameters.AddWithValue("@seq", seq);

            await cmd.ExecuteNonQueryAsync();

            seq++;
        }

        return Ok("Upload TXT sukses + sequence");
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.ToString());
    }
}

    // ================================
    // ✅ GET DATA
    // ================================
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var list = new List<object>();

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var sql = "SELECT id, scan_time, camera_id, qr_code, status FROM scan_results ORDER BY id ASC";

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

    // ================================
    // 📥 EXPORT EXCEL
    // ================================
    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Scan Results");

            ws.Cell(1, 1).Value = "Time";
            ws.Cell(1, 2).Value = "Camera 1";
            ws.Cell(1, 3).Value = "Match/No Match";
            ws.Cell(1, 4).Value = "Camera 2";
            ws.Cell(1, 5).Value = "Match/No Match";
            ws.Cell(1, 6).Value = "Code dari Excel";
            ws.Cell(1, 7).Value = "Hasil Pair";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT scan_time, camera_id, qr_code, status
                FROM scan_results
                ORDER BY id ASC", conn);

            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<dynamic>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    time = reader.GetDateTime(0),
                    camera = reader.GetString(1),
                    qr = reader.IsDBNull(2) ? null : reader.GetString(2),
                    status = reader.GetString(3)
                });
            }

            int rowExcel = 2;

            for (int i = 0; i < list.Count - 2; i++)
            {
                var cam1 = list[i];
                var cam2 = list[i + 1];
                var pair = list[i + 2];

                if (cam1.camera == "CAMERA-01" &&
                    cam2.camera == "CAMERA-02" &&
                    pair.camera == "PAIR")
                {
                    ws.Cell(rowExcel, 1).Value = cam1.time;
                    ws.Cell(rowExcel, 2).Value = cam1.qr;
                    ws.Cell(rowExcel, 3).Value = cam1.status;

                    ws.Cell(rowExcel, 4).Value = cam2.qr;
                    ws.Cell(rowExcel, 5).Value = cam2.status;

                    ws.Cell(rowExcel, 6).Value = pair.qr;
                    ws.Cell(rowExcel, 7).Value = pair.status;

                    rowExcel++;
                    i += 2;
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

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