using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(SQLSafeLoginPoc.Startup))]

namespace SQLSafeLoginPoc
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies"
            });

            app.Map("/logout", logoutApp =>
            {
                logoutApp.Run(async context =>
                {
                    context.Authentication.SignOut("Cookies");
                    context.Response.Redirect("/logout-callback");
                });
            });

            app.Map("/logout-callback", callbackApp =>
            {
                callbackApp.Run(async context =>
                {
                    var responseString = @"
                        <html>
                            <body>
                                <h2>Logged Out Successfully!</h2>
                                <p>This tab will close in <span id='countdown'>3</span> seconds...</p>
                                <script type='text/javascript'>
                                    var countdown = 3;
                                    var countdownElement = document.getElementById('countdown');
                        
                                    var interval = setInterval(function() {
                                        countdown--;
                                        countdownElement.innerText = countdown;
                                        if (countdown <= 0) {
                                            clearInterval(interval);
                                            window.close();
                                        }
                                    }, 1000);
                                </script>
                            </body>
                        </html>";

                    var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                    context.Response.Headers["Content-Length"] = buffer.Length.ToString();

                    await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                });
            });

        }
    }

}
