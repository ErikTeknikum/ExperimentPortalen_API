using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace ExperimentPortalen_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentController : Controller
    {
        MySqlConnection connection;

        public CommentController(IConfiguration config)
        {
            string ip = config["ip"];
            connection = new MySqlConnection(ip);
        }

        [HttpPost]
        public ActionResult CreateComment(Comment comment) //FUNGERAR
        {            
            try
            {
                connection.Open();
                //string userHeader = Request.Headers[""];
                //Lägg till 403 Forbidden statuskod

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO comments (comments.userId, comments.exptId, comments.content) VALUES(@userId, @exptId, @content)";
                command.Parameters.AddWithValue("@userId", comment.userId);
                command.Parameters.AddWithValue("@exptId", comment.exptId);
                command.Parameters.AddWithValue("@content", comment.content);

                if (comment.content.Length < 1)
                {
                    return StatusCode(403, "Kommentar saknar innehåll");
                }

                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(201, $"Lyckades skapa kommentar på inlägg {comment.exptId}, med innehållet {comment.content}");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, $"Lyckades inte skapa kommentar, meddelande: {exception.Message}");
            }
        }
        //Creates a table row connected to a experiment post

        [HttpDelete]
        public ActionResult DeleteComment(int commentId) //FUNGERAR, KOLLAR IFALL DEN FINNS ELLER EJ
        {
            try
            {
                connection.Open();
                string userHeader = Request.Headers[""];

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "SELECT EXISTS(SELECT * FROM comments WHERE comments.id = @commentId1) AS bool;";
                command.Parameters.AddWithValue("@commentId1", commentId); //CHANGE NAME ON COMMENT ID?

                MySqlDataReader data = command.ExecuteReader();
                data.Read();

                if(data.GetInt32("bool") == 1)
                {
                    data.Close();
                    Console.WriteLine("Rad finns i databas!");                    
                }
                else
                {
                    data.Close();
                    return StatusCode(404, "Kommentar finns inte i databas!");
                }

                command.CommandText = "DELETE FROM comments WHERE comments.id = @commentId2";
                command.Parameters.AddWithValue("@commentId2", commentId); //CHANGE NAME ON COMMENT ID?
                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(200, "Lyckades ta bort kommentar!");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Deletes a comment row in database by id, checks beforehand if a comment with given id exists and throws 404 if not.
    }
}
