public interface IMatchClock
{
    // Tiempo global en segundos (sincronizado entre clientes)
    double Now { get; }
}