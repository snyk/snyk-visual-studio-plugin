namespace SnykCode
{
    public class HelloWorld
    {
        public void SayHello()
        {           
            try
            {
                string pass = "mypassword";

                bool isTrue = true;

                if (isTrue) System.Console.WriteLine("My password: " + pass);
            } catch (Exception exception)
            {

            }
        }
    }
}