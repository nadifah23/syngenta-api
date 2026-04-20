// ✅ POST DATA (UNTUK TERIMA DARI SCANNER)
[HttpPost]
public async Task<IActionResult> Post([FromBody] object data)
{
    try
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var doc = System.Text.Json.JsonDocument.Parse(json);

        var root = doc.RootElement;

        var time = root.GetProperty("timestamp").GetDateTime();
        var camera = root.GetProperty("cameraId").GetString();
        var qr = root.TryGetProperty("qrCode", out var qrProp) && qrProp.ValueKind != System.Text.Json.JsonValueKind.Null
            ? qrProp.GetString()
            : null;
        var status = root.GetProperty("status").GetString();

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

        return Ok("Saved from API");
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}