using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace ExperimentPortalen_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LikeController : Controller
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;uid=root;pwd=;database=experiment_portalen");

        [HttpPost]
        public ActionResult CreateLike(int exptId, int userId) //FUNGERAR
        {
            
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO likes (likes.userId, likes.exptId) VALUES(@userId, @exptId)";
                command.Parameters.AddWithValue("@exptId", exptId);
                command.Parameters.AddWithValue("@userId", userId);

                int rows = command.ExecuteNonQuery();

                return StatusCode(200, "Lyckades gilla experiment");
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Adds a row in database to store liked post, single user can't like post more than once due to primary keys in database

        [HttpDelete]
        public ActionResult DeleteLike(int exptId, int userId) //FUNGERAR
        {
            
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "DELETE FROM likes WHERE likes.exptId = @exptId AND likes.userId = @userId";
                command.Parameters.AddWithValue("@exptId", exptId);
                command.Parameters.AddWithValue("@userId", userId);

                int rows = command.ExecuteNonQuery();

                return StatusCode(200, "Lyckades sluta gilla experiment");
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Deletes like row in database
    }
}
