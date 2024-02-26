using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Formats.Asn1;


namespace ExperimentPortalen_API.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class ExperimentController : Controller
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;uid=root;pwd=;database=experiment_portalen");

        [HttpPost]
        public ActionResult CreateExperiment(Experiment experiment)
        {
            try
            {
                connection.Open();
                string userHeader = Request.Headers[""];
                //Lägg till 403 Forbidden statuskod

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO `experiments` (`userId`, `title`, `desc`, `materials`, `instructions`) VALUES(@userId, @title, @desc, @materials, @instructions)";
                command.Parameters.AddWithValue("@userId", experiment.userId);
                command.Parameters.AddWithValue("@title", experiment.title);
                command.Parameters.AddWithValue("@desc", experiment.desc);
                command.Parameters.AddWithValue("@materials", experiment.materials);
                command.Parameters.AddWithValue("@instructions", experiment.instructions);

                if (experiment.title == string.Empty || experiment.desc == string.Empty || experiment.materials == string.Empty)
                {
                    connection.Close();
                    return StatusCode(204, "Kunde inte ladda upp inlägg, inlägg saknar viktig information");
                }

                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(201, $"Lyckades ladda upp inlägg med titel: {experiment.title}");

            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }

        [HttpGet]
        public ActionResult<List<Experiment>> ViewAllExpts()
        {
            List <Experiment> experimentsList = new List <Experiment>();
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "SELECT experiments.id, experiments.userId, experiments.title, experiments.desc, experiments.materials, experiments.instructions FROM experiments ORDER BY experiments.id;"; //gets experiments
                

                MySqlDataReader experimentData = command.ExecuteReader();

                while (experimentData.Read())
                {
                    Experiment experiment = new Experiment();
                    experiment.id = experimentData.GetUInt32("id");
                    experiment.userId = experimentData.GetUInt32("userId");
                    experiment.title = experimentData.GetString("title");
                    experiment.desc = experimentData.GetString("desc");
                    experiment.materials = experimentData.GetString("materials");
                    experiment.instructions = experimentData.GetString("instructions");

                    experimentsList.Add(experiment);
                    
                }
                experimentData.Close();
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }

            foreach (Experiment experiment in experimentsList)
            {
                experiment.comments = getComments(experiment.id);
                experiment.categories = getCategories(experiment.id);
                experiment.imageURLs = getImages(experiment.id);
                experiment.likeCount = Likes(experiment.id);
                experiment.name = GetUserName(experiment.userId);
            }


            if(experimentsList.Count == 0)
            {
               connection.Close();
               return StatusCode(204, "Inga inlägg i databasen");
            }
            connection.Close();
            return StatusCode(200, experimentsList);

           
        }

        private string GetUserName(UInt32 userId)
        {
            MySqlCommand nameCommand = connection.CreateCommand();
            string username = "";

            try
            {
                nameCommand.Prepare();
                nameCommand.CommandText = "SELECT users.name FROM users WHERE users.id = @userId";
                nameCommand.Parameters.AddWithValue("@userId", userId);
                MySqlDataReader nameReader = nameCommand.ExecuteReader();
                nameReader.Read();
                username = nameReader.GetString("name");
                nameReader.Close();
            }
            catch(Exception exception) 
            {
                Console.WriteLine(exception.Message);
            }
            return username;
        }

        private List<Comment> getComments(UInt32 experimentId)
        {
            List<Comment> commentList = new List<Comment>();
            MySqlCommand commentCommand = connection.CreateCommand();
            try
            {
                commentCommand.Prepare();
                commentCommand.CommandText = "SELECT comments.id, comments.userId, comments.exptId, comments.content FROM comments INNER JOIN experiments ON comments.exptId = experiments.id WHERE comments.exptId = @exptId;"; //gets comments
                commentCommand.Parameters.AddWithValue("@exptId", experimentId);
                using (MySqlDataReader commentData = commentCommand.ExecuteReader())
                {

                    while (commentData.Read())
                    {
                        Comment comment = new Comment();
                        comment.id = commentData.GetUInt32("id");
                        comment.exptId = commentData.GetUInt32("exptId");
                        comment.userId = commentData.GetUInt32("userId");
                        comment.content = commentData.GetString("content");
                        commentList.Add(comment);
                    }
                    commentData.Close();

                    foreach(Comment comment in commentList)
                    {
                        comment.name = GetUserName(comment.userId);
                    }
                }

                return commentList;
            }
            catch(Exception exception)
            {
                Console.WriteLine($"Ett serverfel inträffade medans kommentarer hämtades: {exception.Message}");
                return commentList;
            }
          
        }

        private List<Category> getCategories(UInt32 experimentId)
        {
            List<Category> categoryList = new List<Category>();
            MySqlCommand categoryCommand = connection.CreateCommand();
            try
            {
                categoryCommand.Prepare();
                categoryCommand.CommandText = "SELECT categories.id, categories.exptId, categories.category FROM categories INNER JOIN experiments ON categories.exptId = experiments.id WHERE categories.exptId = @exptId";
                categoryCommand.Parameters.AddWithValue("@exptId", experimentId);
                using(MySqlDataReader categoryData = categoryCommand.ExecuteReader())
                {
                    while (categoryData.Read())
                    {
                        Category category = new Category();
                        category.id = categoryData.GetUInt32("id");
                        category.exptId = categoryData.GetUInt32("exptId");
                        category.category = categoryData.GetString("category");
                        categoryList.Add(category);
                    }
                    categoryData.Close();
                }
                return categoryList;
            }
            catch(Exception exception)
            {
                Console.WriteLine($"Ett serverfel inträffade medans categorier hämtades: {exception.Message}");
                return categoryList; 
            }
        }

        private List<ImageURL> getImages(UInt32 experimentId)
        {
            List<ImageURL> urlList = new List<ImageURL>();
            MySqlCommand imageCommand = connection.CreateCommand();
            try
            {
                imageCommand.Prepare();
                imageCommand.CommandText = "SELECT imageurls.id, imageurls.url, imageurls.exptId FROM imageurls INNER JOIN experiments ON imageurls.exptId = experiments.id WHERE imageurls.exptId = @exptId";
                imageCommand.Parameters.AddWithValue("@exptId", experimentId);
                using (MySqlDataReader urlData = imageCommand.ExecuteReader())
                {
                    while (urlData.Read())
                    {
                        ImageURL ímageUrl = new ImageURL();
                        ímageUrl.id = urlData.GetUInt32("id");
                        ímageUrl.url = urlData.GetString("url");
                        ímageUrl.exptId = urlData.GetUInt32("exptId");
                        urlList.Add(ímageUrl);
                    }
                    urlData.Close();
                }
                return urlList;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Ett serverfel inträffade medans bilder hämtades: {exception.Message}");
                return urlList;
            }
        }

        private uint Likes(UInt32 experimentId)
        {

            MySqlCommand likeCommand = connection.CreateCommand();
            uint likes = 0;
            try
            {
                likeCommand.Prepare();
                likeCommand.CommandText = "SELECT COUNT(userId) AS likeCount FROM Likes WHERE exptId = @exptId";
                likeCommand.Parameters.AddWithValue("@exptId", experimentId);

                MySqlDataReader likeData = likeCommand.ExecuteReader();
                likeData.Read();
                likes = likeData.GetUInt32("likeCount");
                Console.WriteLine("antal gillningar" + likes);
                
                likeData.Close();
                
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            return likes;
        }

        [HttpGet("{experimentId}")]
        public ActionResult GetSingleExperiment(int experimentId)
        {
            try
            {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "SELECT experiments.id, experiments.userId, experiments.title, experiments.desc, experiments.materials, experiments.instructions FROM experiments WHERE experiments.id = @experimentId;";
                command.Parameters.AddWithValue("@experimentId", experimentId);
                MySqlDataReader experimentData = command.ExecuteReader();

                if(experimentData.HasRows == false)
                {
                    return StatusCode(404, "Kunde inte hitta experiment-inlägg i databasen");
                }

                Experiment experiment = new Experiment();
                while (experimentData.Read())
                {
                    experiment.id = experimentData.GetUInt32("id");
                    experiment.userId = experimentData.GetUInt32("userId");
                    experiment.title = experimentData.GetString("title");
                    experiment.desc = experimentData.GetString("desc");
                    experiment.materials = experimentData.GetString("materials");
                    experiment.instructions = experimentData.GetString("instructions");
                }
                experimentData.Close();

                experiment.comments = getComments(experiment.id);
                experiment.imageURLs = getImages(experiment.id);
                experiment.categories = getCategories(experiment.id);
                experiment.likeCount = Likes(experiment.id);      
                experiment.name = GetUserName(experiment.id);

                connection.Close();
                return StatusCode(200, experiment);

            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
    }
}

