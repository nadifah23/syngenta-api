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

    // ✅ POST
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
            // 🔥 INI YANG PALING PENTING
            Console.WriteLine("❌ ERROR API:");
            Console.WriteLine(ex.ToString());

            return StatusCode(500, ex.ToString());
        }
    }

    // ✅ GET TEST
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