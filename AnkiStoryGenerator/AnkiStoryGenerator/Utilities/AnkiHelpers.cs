using System.Data.SQLite;

namespace AnkiStoryGenerator.Utilities;

public record Flashcard(string WordInLearnedLanguage, string WordInUserNativeLanguage);

/// <summary>
/// Allows to read relevant data from Anki's SQLite database and returns them as .NET objects.
/// </summary>
public static class AnkiHelpers
{
    /// <summary>
    /// Returns a list of flashcards that were recently reviewed by user.
    /// </summary>
    /// <param name="databaseFilePath">A filesystem path to a SQL database of Anki (typically named `collection.anki2`)</param>
    /// <param name="deckName">Name of the deck serving as a filter for which flashcards to retrieve.</param>
    /// <param name="numRecentCardsToFetch">A number of cards to fetch.</param>
    /// <returns></returns>
    public static List<Flashcard> GetRecentlyReviewedCardsFromSpecificDeck(string databaseFilePath, string deckName, int numRecentCardsToFetch)
    {
        using var connection = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;");
        connection.Open();

        var query = $@"
            SELECT DISTINCT
                notes.flds
            FROM
                cards
            JOIN
                notes
            ON
                cards.nid = notes.id
            JOIN
                revlog
            ON
                cards.id = revlog.cid
            WHERE
                cards.did = (SELECT id FROM decks WHERE name COLLATE NOCASE = '{deckName}')
            ORDER BY
                revlog.id DESC
            LIMIT {numRecentCardsToFetch}
        ";

        using var command = new SQLiteCommand(query, connection);
        using var reader = command.ExecuteReader();

        var flashcards = new List<Flashcard>();
        while (reader.Read())
        {
            var fields = reader.GetString(0).Split('\x1f');

            // quick and hackish way to recognize fields in the card and what they mean (only in PoC)

            // My typical deck, including Spanish
            var wordInLearnedLanguage = fields[0];
            var wordInUserNativeLanguage = fields[2];

            if (fields.Length == 5) // My English deck 
            {
                wordInUserNativeLanguage = fields[0];
                wordInLearnedLanguage = fields[1];
            }

            // todo: there is a room for heuristics, e.g. for flashcards that have `Sentences with <b>Some Word</b>` highlighted, I can isolate that word

            flashcards.Add(new Flashcard(wordInLearnedLanguage, wordInUserNativeLanguage));
        }

        return flashcards;
    }
}
