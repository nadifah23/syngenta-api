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
            Console.WriteLine("📥 DATA MASUK:");
            Console.WriteLine($"time: {data.timestamp}");
            Console.WriteLine($"camera: {data.cameraId}");
            Console.WriteLine($"qr: {data.qrCode}");
            Console.WriteLine($"status: {data.status}");

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

            Console.WriteLine("✅ BERHASIL INSERT DB");

            return Ok("Saved");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ ERROR API:");
            Console.WriteLine(ex.ToString());

            return StatusCode(500, ex.ToString());
        }
    }

    // ================================
    // 🔥 UPLOAD DATASHEET (INI BARU)
    // ================================
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("File kosong");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);

            await using var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();

            // 🔥 hapus data lama (biar update)
            var clear = new NpgsqlCommand("DELETE FROM qr_references", conn);
            await clear.ExecuteNonQueryAsync();

            int row = 2;

            while (!ws.Cell(row, 1).IsEmpty())
            {
                var qr = ws.Cell(row, 1).GetString();

                var cmd = new NpgsqlCommand(
                    "INSERT INTO qr_references (qr_code) VALUES (@qr)", conn);

                cmd.Parameters.AddWithValue("@qr", qr);
                await cmd.ExecuteNonQueryAsync();

                row++;
            }

            Console.WriteLine("✅ UPLOAD DATASHEET BERHASIL");

            return Ok("Upload sukses");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ ERROR UPLOAD:");
            Console.WriteLine(ex.ToString());

            return StatusCode(500, ex.ToString());
        }
    }

    // ================================
    // ✅ GET DATA (DASHBOARD)
    // ================================
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
}