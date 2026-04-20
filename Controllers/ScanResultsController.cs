using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ClosedXML.Excel;

namespace Syngenta.API.Controllers;


[HttpPost]
public async Task<IActionResult> Post([FromBody] dynamic data)
{
    try
    {
        DateTime time = data.timestamp;
        string camera = data.cameraId;
        string qr = data.qrCode;
        string status = data.status;

        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();

        var sql = @"
            INSERT INTO scan_results (scan_time, camera_id, qr_code, status)
            VALUES (@time, @camera, @qr, @status)";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@time", time);
        cmd.Parameters.AddWithValue("@camera", camera ?? "");
        cmd.Parameters.AddWithValue("@qr", (object?)qr ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", status ?? "");

        await cmd.ExecuteNonQueryAsync();

        return Ok("Saved");
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}