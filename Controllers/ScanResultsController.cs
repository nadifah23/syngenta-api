using Syngenta.API.Models;

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