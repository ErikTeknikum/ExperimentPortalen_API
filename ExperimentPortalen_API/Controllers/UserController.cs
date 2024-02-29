using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Pkix;

//https://refactoring.guru/design-patterns/builder/csharp/example#:~:text=Builder%20is%20a%20creational%20design,using%20the%20same%20construction%20process.
//Check link to see how to make builder

//Authorization is made with google oAuth 2.0
//Check resources for more information

namespace ExperimentPortalen_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;uid=root;pwd=;database=experiment_portalen");

        [HttpPost]
        public ActionResult CreateUser(string name, string email, string pwd)
        {
            try
            {
                string hashedPwd = BCrypt.Net.BCrypt.HashPassword(pwd);

                UserBuilder builder = new UserBuilder();
                builder.Email(email).Name(name).Password(hashedPwd);
                User userbuild = builder.Build();

                string checkUniqueUser = CheckIfUniqueUserDataExists(userbuild);
                if (checkUniqueUser != String.Empty)
                {
                    connection.Close();
                    return StatusCode(500, checkUniqueUser);
                }

                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "INSERT INTO users (users.name, users.email, users.pwd, users.role) VALUES(@name, @email, @pwd, 1)";
                command.Parameters.AddWithValue("@name", userbuild.name);
                command.Parameters.AddWithValue("@email", userbuild.email);
                command.Parameters.AddWithValue("@pwd", userbuild.password);

                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(201, "Lyckades skapa användare");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, $"Kunde inte skapa användare på grund av serverfel: {exception.Message}");
            }
        }

        [HttpDelete]
        public ActionResult DeleteUser(int id) //EJ FÄRDIG KOLLA ADMIN ROLL FÖRST, VÄNTA TILLS FRONTEND FUNGERAR
        {
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "DELETE FROM user WHERE user.id = @id";
                command.Parameters.AddWithValue("@userId", id);

                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(200, $"Lyckads ta bort användare med id:{id}");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }

        private string CheckIfUniqueUserDataExists(User user)
        {
            string checkUniqueUser = String.Empty;
            try
            {
                MySqlCommand query = connection.CreateCommand();
                query.Prepare();
                query.CommandText = "SELECT * FROM user WHERE email = @userEmail OR name = @userName";
                query.Parameters.AddWithValue("@userName", user.name);
                query.Parameters.AddWithValue("@userEmail", user.email);
                MySqlDataReader data = query.ExecuteReader();

                if (data.Read())
                {
                    if (data.GetString("email") == user.email)
                    {
                        checkUniqueUser = "Email används redan på hemsidan";
                    }
                    if (data.GetString("name") == user.name)
                    {
                        checkUniqueUser = "Användarnamn används redan på hemsidan";
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"UserController.CheckIfUniqueUserDataExists: {exception.Message}");
                connection.Close();
            }

            return checkUniqueUser;
        }

    }
}
