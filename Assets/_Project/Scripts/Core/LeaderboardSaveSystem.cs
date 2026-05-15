using System;
using UnityEngine;

// Хранилище лидерборда: топ-5 через PlayerPrefs + JsonUtility.
// Ключ содержит версию (v1) — при изменении формата меняем версию, старые
// записи остаются нетронутыми. Сортировка по score по убыванию.
public static class LeaderboardSaveSystem
{
    const string Key = "leaderboard_v1";
    public const int TopSize = 5;

    public static LeaderboardData Load()
    {
        // 1. Десериализация; если ключа нет — пустой контейнер
        var json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json)) return new LeaderboardData();

        try
        {
            var data = JsonUtility.FromJson<LeaderboardData>(json);
            if (data == null) return new LeaderboardData();
            if (data.entries == null)
                data.entries = new System.Collections.Generic.List<LeaderboardEntry>();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Leaderboard] Ошибка чтения, начинаем с пустого. "
                + e.Message);
            return new LeaderboardData();
        }
    }

    public static void Save(LeaderboardData data)
    {
        if (data == null) return;
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static bool QualifiesForTop(int score)
    {
        // 2. Попадает в топ, если мест меньше TopSize ИЛИ счёт выше худшего
        var data = Load();
        if (data.entries.Count < TopSize) return score > 0;
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        return score > data.entries[TopSize - 1].score;
    }

    public static void Submit(string name, int score, string difficulty)
    {
        var data = Load();
        data.entries.Add(new LeaderboardEntry
        {
            name = string.IsNullOrWhiteSpace(name) ? "Игрок" : name,
            score = score,
            difficulty = difficulty ?? string.Empty,
            date = DateTime.UtcNow.ToString("o"),
        });
        // 3. Сортируем по очкам по убыванию и обрезаем до TopSize
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (data.entries.Count > TopSize)
            data.entries.RemoveRange(TopSize, data.entries.Count - TopSize);
        Save(data);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
