using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Pkix;
using System.Collections;
using System.Text;

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

        public static Hashtable sessionId = new Hashtable();

        [HttpPost]
        public ActionResult CreateUser(string name, string email, string pwd)
        {
            try
            {
                string hashedPwd = BCrypt.Net.BCrypt.HashPassword(pwd);

                UserBuilder builder = new UserBuilder();
                builder.Email(email).Name(name).Password(hashedPwd);
                User userbuild = builder.Build();
                connection.Open();
                string checkUniqueUser = CheckIfUniqueUserDataExists(userbuild);
                if (checkUniqueUser != String.Empty)
                {
                    connection.Close();
                    return StatusCode(500, checkUniqueUser);
                }

                
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
            string? auth = this.HttpContext.Request.Headers["Authorization"];

            //If user is not logged in returns 403 Forbidden
            if (auth == null || !UserController.sessionId.ContainsKey(auth))
            {
                return StatusCode(403, "0");
            }
            User user = (User)UserController.sessionId[auth];

            //If user is not admin role returns 403 Forbidden
            if (user.role != 3)
            {
                return StatusCode(403, "Du har inte behörighet!");
            }

            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "DELETE FROM users WHERE users.id = @id";
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
                query.CommandText = "SELECT * FROM users WHERE email = @userEmail OR name = @userName";
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
                data.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"UserController.CheckIfUniqueUserDataExists: {exception.Message}");
                
            }

            return checkUniqueUser;
        }

        [HttpGet("VerifyUserId")]
        public ActionResult VerifyUserId()
        {
            string auth = this.HttpContext.Request.Headers["Authorization"];
            if (auth == null || !UserController.sessionId.ContainsKey(auth))
            {
                return StatusCode(403, "0");
            }
            User user = (User)UserController.sessionId[auth];
            return StatusCode(200, user.id);
        }

        [HttpGet("Login")] //ALL FUNCTION
        public ActionResult Login() //KOLLA IFALL REDAN INLOGGAD, 
        {
            string? auth = this.HttpContext.Request.Headers["Authorization"] ; //testa ifall string? inte ställer till det
            User user = DecodeUser(new UserBuilder(), auth);
            connection.Open();
            MySqlCommand command = connection.CreateCommand();
            command.Prepare();
            command.CommandText = "SELECT * FROM users WHERE email = @email";
            command.Parameters.AddWithValue("@email", user.email); //Lägg till så name också kan användas?
            MySqlDataReader data = command.ExecuteReader();
            try
            {

                string passwordHash = String.Empty;

                while (data.Read())
                {
                    passwordHash = data.GetString("pwd");
                    user.id = data.GetUInt32("id"); //Onödig?
                    user.email = data.GetString("email"); //Onödig
                    user.role = data.GetUInt32("role"); //Onödig?
                }
                data.Close();

                if (passwordHash != string.Empty && BCrypt.Net.BCrypt.Verify(user.password, passwordHash))
                {
                    Guid guid = Guid.NewGuid();
                    string key = guid.ToString();
                    Console.WriteLine(key); //Onödig
                    sessionId.Add(key, user);
                    connection.Close();
                    return Ok(key);
                }

                connection.Close();
                return StatusCode(400);
            }
            catch (Exception exception)
            {
                connection.Close();
                Console.WriteLine($"Login failed: {exception.Message}");
                return StatusCode(500);
            }
        }

        private User DecodeUser(UserBuilder userbuilder, string? auth) //Ingen aning om detta kommer fungera
        {
            if (auth != null && auth.StartsWith("Basic"))
            {
                string encodedUsernamePassword = auth.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("UTF-8");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
                int seperatorIndex = usernamePassword.IndexOf(':');
                userbuilder.Email(usernamePassword.Substring(0, seperatorIndex));
                userbuilder.Password(usernamePassword.Substring(seperatorIndex + 1));

                return (User)userbuilder.Build();
            }
            else
            {
                //Handle what happens if that isn't the case
                throw new Exception("The authorization header is either empty or isn't Basic.");
            }
        }
    }
}
