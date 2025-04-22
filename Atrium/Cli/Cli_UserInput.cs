namespace Atrium
{


    internal enum InputMode
    {
        /// <summary>
        /// La normale modalità di inserimento comandi.
        /// </summary>
        UserCommand,
        /// <summary>
        /// Modalità speciale di inserimento input.
        /// Viene utilizzata quando l'utente deve fornire un input ad un comando in esecuzione.
        /// </summary>
        UserCommandInput,
        /// <summary>
        /// Modalità speciale di inserimento input.
        /// Viene utilizzata quando l'utente deve fornire una password in input ad un comando in esecuzione.
        /// </summary>
        UserCommandInputPassword,
        /// <summary>
        /// L'input dell'utente è bloccato, soltanto il sistema può scrivere nella console.
        /// </summary>
        SystemText
    }


}
