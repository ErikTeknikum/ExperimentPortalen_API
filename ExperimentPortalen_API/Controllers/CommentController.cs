using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace ExperimentPortalen_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentController : Controller
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;uid=root;pwd=;database=experiment_portalen");

        [HttpPost]
        public ActionResult createComment(Comment comment) //FUNGERAR EJ || CONNECTION POPERTY HAS NOT BEEN SET
        {            
            try
            {
                connection.Open();
                //string userHeader = Request.Headers[""];
                //Lägg till 403 Forbidden statuskod

                MySqlCommand command = new MySqlCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO comments (comments.userId, comments.exptId, comments.content) VALUES(@userId, @exptId, @content)";
                command.Parameters.AddWithValue("@userId", comment.userId);
                command.Parameters.AddWithValue("@exptId", comment.exptId);
                command.Parameters.AddWithValue("@content", comment.content);

                if (comment.content.Length > 1)
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
                return StatusCode(500, $"Lyckades inte skapa kommentar på grund av serverfel {exception.Message}");
            }
        }

        [HttpDelete]
        public ActionResult deleteComment(int commentId) //FUNGERAR EJ || CONNECTION POPERTY HAS NOT BEEN SET
        {
            try
            {
                connection.Open();
                string userHeader = Request.Headers[""];

                MySqlCommand command = new MySqlCommand();
                command.Prepare();
                command.CommandText = "DELETE FROM comments WHERE comments.id = @commentId";
                command.Parameters.AddWithValue("@commentId", commentId);
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
    }
}
