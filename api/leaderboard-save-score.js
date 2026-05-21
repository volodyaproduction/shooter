import { cmd, pipeline, readJsonBody,
         KEY_SCORES, KEY_RATE } from "./_redis.js";

// POST /api/leaderboard-save-score  { player_id, score }
//
// Защита от накрутки:
// (1) Sanity cap: счёт > MAX_SCORE отсекается на сервере. Теоретический
//     максимум за 30-секундный раунд считается из параметров игры
//     (см. DifficultyConfig "normal" + бонус за реакцию).
// (2) Rate limit: один сабмит на player_id раз в 25 секунд. SET NX EX 25 —
//     ключ-стопор, существует только окно длительностью раунда.
//
// ZADD ... GT обновляет счёт только если новый больше предыдущего, поэтому
// сабмит «хуже моего рекорда» не двигает позицию в топе, но мы всё равно
// возвращаем игроку его personal best — для надписи «Твой рекорд: X».
const MAX_SCORE = 2000;
const RATE_WINDOW = 25;

export default async function handler(req, res) {
  if (req.method !== "POST") {
    res.status(405).json({ error: "method_not_allowed" });
    return;
  }
  const body = readJsonBody(req);
  if (!body) {
    res.status(400).json({ error: "bad_request" });
    return;
  }
  const playerId = String(body.player_id || "").trim();
  const score = Number(body.score);

  // 1. Валидация формата
  if (!playerId || !/^[a-f0-9]{32}$/i.test(playerId)) {
    res.status(400).json({ error: "invalid_player_id" });
    return;
  }
  if (!Number.isFinite(score) || score < 0 || !Number.isInteger(score)) {
    res.status(400).json({ error: "invalid_score" });
    return;
  }

  // 2. Sanity cap — отсекает «999999»
  if (score > MAX_SCORE) {
    res.status(400).json({ error: "score_too_high" });
    return;
  }

  try {
    // 3. Rate limit — атомарный SET NX EX
    const rateOk = await cmd(["SET", KEY_RATE(playerId), "1",
      "EX", String(RATE_WINDOW), "NX"]);
    if (rateOk === null) {
      res.status(429).json({ error: "rate_limited" });
      return;
    }

    // 4. Записываем счёт (только если новый рекорд) и сразу читаем PB
    const [, pbRaw] = await pipeline([
      ["ZADD", KEY_SCORES, "GT", String(score), playerId],
      ["ZSCORE", KEY_SCORES, playerId],
    ]);
    const personalBest = pbRaw === null ? score : Number(pbRaw);
    const isNewRecord = personalBest === score;

    res.status(200).json({
      personalBest,
      isNewRecord,
    });
  } catch (e) {
    res.status(500).json({ error: "server_error", message: String(e.message) });
  }
}
