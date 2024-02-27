using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

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

        public ActionResult CreateUser()
        {
            try
            {
                
                

                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO users (users.name, users.email, users.pwd, users.role) VALUES(@name, @email, @pwd, 1)";

                connection.Close();
                return StatusCode(201, "Lyckades skapa användare");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, "Kunde inte skapa användare på grund av serverfel");
            }


        }

    }
}
