# Einführung 
TODO: Geben Sie eine kurze Einführung in Ihr Projekt. In diesem Abschnitt erklären Sie die Ziele oder die Motivation hinter diesem Projekt. 

# Erste Schritte
TODO: Leiten Sie die Benutzer durch die Inbetriebnahme Ihres Codes auf deren Systemen. In diesem Abschnitt können Sie folgende Themen behandeln:
1.	Startup:
            services.AddSingleton<AlexaSkillHandler.Models.AlexaHandler>(new AlexaSkillHandler.Models.AlexaHandler());
            services.AddSingleton<MusicService>(new MusicService());
            
            app.UseAlexaHandler(typeof(Controllers.AlexaController)); //Controller der für Alexa benutzt werden soll
2.	Controller mit Injection "AlexaHandler handler" benutzen
3.	Return SkillResponse