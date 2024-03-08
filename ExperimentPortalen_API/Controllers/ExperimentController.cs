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

        [HttpGet]
        public ActionResult<List<Experiment>> GetAllExpts() //GETS ALL EXPERIMENTS
        {
            List<Experiment> experimentsList = new List<Experiment>();
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "SELECT experiments.id, experiments.userId, experiments.title, " +
                    "experiments.desc, experiments.materials, experiments.instructions" +
                    " FROM experiments ORDER BY experiments.id;"; //gets experiments

                MySqlDataReader experimentData = command.ExecuteReader();

                while (experimentData.Read())
                {
                    Experiment experiment = new Experiment(); //Skapa metod för detta?
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
                experiment.comments = GetComments(experiment.id);
                experiment.categories = GetCategories(experiment.id);
                experiment.imageURLs = GetImages(experiment.id);
                experiment.likeCount = Likes(experiment.id);
                experiment.name = GetUserName(experiment.userId);
            }
            
            connection.Close();

            if (experimentsList.Count == 0)
            {
                return StatusCode(204, "Inga inlägg i databasen");
            }            
            return StatusCode(200, experimentsList);
        }
        //Gets all experiments in database, along with comments, likes, categories and imageurls

        [HttpGet("{exptId}")] //GET SINGLE EXPERIMENT
        public ActionResult GetSingleExperiment(int exptId)
        {
            try
            {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "SELECT experiments.id, experiments.userId, experiments.title, " +
                    "experiments.desc, experiments.materials, experiments.instructions " +
                    "FROM experiments WHERE experiments.id = @experimentId;";
                command.Parameters.AddWithValue("@experimentId", exptId);

                MySqlDataReader experimentData = command.ExecuteReader();

                if (experimentData.HasRows == false)
                {
                    return StatusCode(404, "Kunde inte hitta experiment-inlägg i databasen");
                }

                Experiment experiment = new Experiment();
                experimentData.Read();

                experiment.id = experimentData.GetUInt32("id");
                experiment.userId = experimentData.GetUInt32("userId");
                experiment.title = experimentData.GetString("title");
                experiment.desc = experimentData.GetString("desc");
                experiment.materials = experimentData.GetString("materials");
                experiment.instructions = experimentData.GetString("instructions");

                experimentData.Close();

                experiment.comments = GetComments(experiment.id);
                experiment.imageURLs = GetImages(experiment.id);
                experiment.categories = GetCategories(experiment.id);
                experiment.likeCount = Likes(experiment.id);
                experiment.name = GetUserName(experiment.userId);

                connection.Close();
                return StatusCode(200, experiment);
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Used to get a single post in database from exptId

        [HttpPut]
        public ActionResult EditExperiment(Experiment experiment) //Färdig
        {
            try
            {
                connection.Open();
                //string userHeader = Request.Headers[""];
                //Lägg till 403 Forbidden statuskod

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "UPDATE `experiments` SET `title` = @title, " +
                    "`desc` = @desc, `materials` = @materials, `instructions` = @instructions " +
                    "WHERE experiments.id = @exptId";
                command.Parameters.AddWithValue("@exptId", experiment.id);
                command.Parameters.AddWithValue("@title", experiment.title);
                command.Parameters.AddWithValue("@desc", experiment.desc);
                command.Parameters.AddWithValue("@materials", experiment.materials);
                command.Parameters.AddWithValue("@instructions", experiment.instructions);

                //Ändra ifsats till funktion
                if (experiment.title == string.Empty || experiment.desc == string.Empty || experiment.materials == string.Empty)
                {
                    connection.Close();
                    return StatusCode(204, "Kunde inte ladda upp inlägg, inlägg saknar viktig information");
                }

                int rows = command.ExecuteNonQuery();                

                DeleteImages(experiment.id);
                PostImageUrls(experiment);
                //deletes current images in database then posts the new/updated images

                DeleteCategories(experiment.id);
                PostCategories(experiment);
                //deletes current categories in database then posts the new/updated categories

                connection.Close();
                return StatusCode(201, $"Lyckades ladda upp inlägg med titel: {experiment.title}");

            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Edits and existing experiment, calls EditImageUrls and EditCategories to change connected urls and categories in database

        [HttpDelete("images")]
        public ActionResult DeleteImages(uint exptId)
        {
            try
            {
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "SELECT COUNT(*) AS count FROM imageurl WHERE exptId = @exptId";
                command.Parameters.AddWithValue("@exptId", exptId);
                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();
                
                int imageCount = reader.GetInt32("count");

                reader.Close();

                if(imageCount < 1)
                {
                    connection.Close();
                    return StatusCode(204, $"Finns inga bilder i databas kopplat till experimentid: {exptId}");
                }
                else
                {
                    command.CommandText = "DELETE FROM imageurl WHERE exptId = @exptId";
                    command.Parameters.AddWithValue("@exptId", exptId);

                    int rows = command.ExecuteNonQuery();

                    return StatusCode(200, "Lyckades ta bort bilder");
                }
            }
            catch (Exception exception)
            {                
                return StatusCode(500, $"Serverfel inträffade när bilder skulle tas bort under ändring {exception.Message}");
            }
        }

        [HttpDelete("Categories")]
        public ActionResult DeleteCategories(uint exptId)
        {
            try
            {
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();

                command.CommandText = "SELECT COUNT(*) FROM categories WHERE exptId = @exptId";
                command.Parameters.AddWithValue("@exptId", exptId);

                MySqlDataReader reader = command.ExecuteReader();
                reader.Read();

                int categoryCount = reader.GetInt32("count");
                if(categoryCount < 1)
                {
                    connection.Close();
                    return StatusCode(404, $"Kunde inte ta bort kategorier, finns ingen kategori kopplad till experiment id: {exptId}");
                }
                else
                {
                    command.CommandText = "DELETE FROM categories WHERE exptId = @exptId";
                    command.Parameters.AddWithValue("@exptId", exptId);
                    int rows = command.ExecuteNonQuery();

                    connection.Close();
                    return StatusCode(200, "Lyckades ta bort gamla bilder");
                }
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }

        [HttpPost]
        public ActionResult CreateExperiment(Experiment experiment) //FÄRDIG
        {
            try
            {
                connection.Open();
                //string userHeader = Request.Headers[""];
                //Lägg till 403 Forbidden statuskod

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO `experiments` (`userId`, `title`, `desc`, `materials`, `instructions`) " +
                    "VALUES(@userId, @title, @desc, @materials, @instructions)";
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

                PostImageUrls(experiment);
                PostCategories(experiment);                

                return StatusCode(201, $"Lyckades ladda upp inlägg med titel: {experiment.title}");
            }
            catch (Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Creates experiment posts, calls other actions to add imageurls and categories to post in database

        //Change to private list?
        [HttpPost("PostImages")]
        public ActionResult<List<ImageURL>> PostImageUrls(Experiment experiment)
        {
            List<ImageURL> images = new List<ImageURL>();
            foreach(ImageURL image in experiment.imageURLs)
            {
                images.Add(image);

                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.Prepare();

                    command.CommandText = "INSERT INTO imageurls (imageurls.exptId, imageurls.url) " +
                        "SELECT MAX(experiments.id), @url FROM experiments";
                    command.Parameters.AddWithValue("@url",  image.url);

                    int rows = command.ExecuteNonQuery();
                }
                catch (Exception exception)
                {
                    connection.Close();
                    return StatusCode(500, exception.Message);
                }
            }
            if(images.Count == 0)
            {
                Console.WriteLine("INGEN BILD HITTADES!! GRR");
                return StatusCode(204, "No images found");
            }
            else
            {
                connection.Close();
                return StatusCode(201, "Lyckades lägga till bild(er)");
            }            
        }
        //Only used in CreateExperiment and EditExperiment Actions to add/change (respectively) imageurls for experiments in database

        //change to private list, not action
        [HttpPost("HEHEHEHEEJAOIHFAFHAHFAÖOIFYAOYFHA")]
        public ActionResult<List<Category>> PostCategories(Experiment experiment)
        {
            List<Category> categories = new List<Category>();

            foreach(Category category in experiment.categories)
            {
                categories.Add(category);

                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.Prepare();
                    command.CommandText = "INSERT INTO categories (categories.exptId, categories.category) " +
                        "SELECT MAX(experiments.id), @category FROM experiments";
                    command.Parameters.AddWithValue("@category", category.category);

                    int rows = command.ExecuteNonQuery();

                    //Endast 1 return, med variabler istället för hårdkodat.

                    connection.Close();
                    return StatusCode(201, "Lyckades lägga till categori");
                }
                catch (Exception exception)
                {
                    connection.Close();
                    return StatusCode(500, exception.Message);
                }
            }
            if(categories.Count == 0)
            {
                connection.Close();
                return StatusCode(204, "No categories found");
            }
            else
            {
                connection.Close();
                return StatusCode(201, "Lyckades lägga till categori(er)");
            }
        }
        //Only used in CreateExperiment and EditExperiment Actions to add/change (respectively) categories for experiments in database

        [HttpPost("Report")]
        public ActionResult ReportExperiment(int userId, int exptId)
        {
            try
            {
                connection.Open();

                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "INSERT INTO reports (reports.userId, reports.exptId) VALUES(@userId, @exptId)";
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@exptId", exptId);

                int rows = command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(201, $"Lyckades rapportera inlägg med id:{exptId}");
            }
            catch(Exception exception) 
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Creates a report to an experiment in the database

        [HttpGet("ReportedExperiments")]
        public ActionResult<List<Experiment>> ReportedExperiment()
        {
            List<Experiment> experimentsList = new List<Experiment>();
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.Prepare();
                command.CommandText = "SELECT experiments.*, COUNT(reports.exptId) AS reportCount FROM experiments " +
                    "JOIN reports ON experiments.id = reports.exptId GROUP BY experiments.id ORDER BY reportCount DESC;";

                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Experiment experiment = new Experiment();
                    experiment.id = reader.GetUInt32("id");
                    experiment.userId = reader.GetUInt32("userId");
                    experiment.title = reader.GetString("title");
                    experiment.desc = reader.GetString("desc");
                    experiment.materials = reader.GetString("materials");
                    experiment.instructions = reader.GetString("instructions");
                    experiment.reportCount = (uint)reader.GetInt32("reportCount");

                    experimentsList.Add(experiment);
                }
                reader.Close();

                connection.Close();
                return StatusCode(200, experimentsList);                
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
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
        //Used in GetComments and ViewAllExpts Actions in order to get usernames from userids

        private List<Comment> GetComments(UInt32 experimentId)
        {
            List<Comment> commentList = new List<Comment>();
            MySqlCommand commentCommand = connection.CreateCommand();
            try
            {
                commentCommand.Prepare();
                commentCommand.CommandText = "SELECT comments.id, comments.userId, comments.exptId, comments.content FROM comments " +
                    "INNER JOIN experiments ON comments.exptId = experiments.id WHERE comments.exptId = @exptId;"; //gets comments
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
        //Used in GetAllExpts Action in order to get all comments connected to posts

        private List<Category> GetCategories(UInt32 experimentId)
        {
            List<Category> categoryList = new List<Category>();
            MySqlCommand categoryCommand = connection.CreateCommand();
            try
            {
                categoryCommand.Prepare();
                categoryCommand.CommandText = "SELECT categories.id, categories.exptId, categories.category FROM categories " +
                    "INNER JOIN experiments ON categories.exptId = experiments.id WHERE categories.exptId = @exptId";
                categoryCommand.Parameters.AddWithValue("@exptId", experimentId);
                using(MySqlDataReader categoryData = categoryCommand.ExecuteReader()) //Okonsekvent kod, ändra
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
            }
            catch(Exception exception)
            {
                Console.WriteLine($"Ett serverfel inträffade medans categorier hämtades: {exception.Message}");                
            }
            return categoryList;
        }
        //Used in GetAllExpts Action in order to get categories connected to posts

        private List<ImageURL> GetImages(UInt32 experimentId)
        {
            List<ImageURL> urlList = new List<ImageURL>();
            MySqlCommand imageCommand = connection.CreateCommand();
            try
            {
                imageCommand.Prepare();
                imageCommand.CommandText = "SELECT imageurls.id, imageurls.url, imageurls.exptId FROM imageurls " +
                    "INNER JOIN experiments ON imageurls.exptId = experiments.id WHERE imageurls.exptId = @exptId";
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
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Ett serverfel inträffade medans bilder hämtades: {exception.Message}");                
            }
            return urlList;
        }
        //Used in GetAllExpts Action in order to get image urls connected to posts

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
        //Used in GetAllExpts Action in order to get the number of likes connected to posts
             
        [HttpDelete("{exptId}")]
        public ActionResult DeleteExperiment(int exptId) //Kolla ifall användare är admin eller har samma userId som experimentet
        {
            try
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM experiments WHERE experiments.id = @exptId";
                command.Parameters.AddWithValue("@id", exptId);
                command.ExecuteNonQuery();

                connection.Close();
                return StatusCode(200, "Lyckades ta bort experiment!");
            }
            catch(Exception exception)
            {
                connection.Close();
                return StatusCode(500, exception.Message);
            }
        }
        //Used to delete a single post in database from exptId
    }
}

