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
        public ActionResult createComment(Comment comment)
        {            
            try
            {
                connection.Open();
                string userHeader = Request.Headers[""];
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

                command.ExecuteNonQuery();

                return StatusCode(201, $"Lyckades skapa kommentar på inlägg {comment.exptId}, med innehållet {comment.content}");
            }
            catch(Exception exception)
            {
                return StatusCode(500, $"Lyckades inte skapa kommentar på grund av serverfel {exception.Message}");
            }
        }
    }
}
