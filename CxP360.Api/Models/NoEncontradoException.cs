namespace CxP360.Api.Models;

/// <summary>El registro solicitado no existe (la API responde 404).</summary>
public class NoEncontradoException(string mensaje) : Exception(mensaje);
