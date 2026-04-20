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
        "Host=...;Port=5432;Database=postgres;Username=postgres;Password=...;SSL Mode=Require;Trust Server Certificate=true;";

    // ✅ POST
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
            return StatusCode(500, ex.Message);
        }
    }

    // ✅ GET tetap ada
    [HttpGet]
    public IActionResult Test()
    {
        return Ok("API OK");
    }
}