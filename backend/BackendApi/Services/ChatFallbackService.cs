using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BackendApi.Services
{
    // Very small rule-based fallback chat bot (no external deps)
    public class ChatFallbackService
    {
        private readonly Random _rng = new Random();

        public string Respond(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Please say something so I can respond.";

            var text = input.Trim().ToLowerInvariant();

            // simple patterns similar to the NLTK rules provided
            if (Regex.IsMatch(text, "\\bhello\\b|\\bhi\\b|\\bhey\\b"))
            {
                return Pick(new[] { "Hello!", "Hi there!", "Hey!" });
            }

            if (Regex.IsMatch(text, "how are you"))
            {
                return Pick(new[] { "I am doing well, thank you!", "I'm great, thanks for asking!" });
            }

            if (Regex.IsMatch(text, "your name"))
            {
                return Pick(new[] { "I am an NLP chatbot.", "You can call me ChatBot." });
            }

            if (Regex.IsMatch(text, "\\b(exit|bye|goodbye)\\b"))
            {
                return Pick(new[] { "Goodbye!", "Bye!", "Take care!" });
            }

            // fallback reply
            return "I'm not sure how to answer that, but I can help with basic questions like greetings or asking my name.";
        }

        private string Pick(string[] options)
        {
            if (options == null || options.Length == 0) return string.Empty;
            return options[_rng.Next(options.Length)];
        }
    }
}
