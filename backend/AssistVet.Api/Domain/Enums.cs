namespace AssistVet.Api.Domain;

public enum TipoLocal { Clinica, Haras, Hipica, Residencia, Outro }

public enum Especie { Cao, Gato, Equino, Outro }

public enum Sexo { Macho, Femea }

public enum TipoPessoa { Fisica, Juridica }

public enum Olho { OD, OE, AO }

public enum Prioridade { Rotina, Ate48h, Urgente }

/// <summary>Quem preencheu a solicitação. Uso interno, nunca mostrado ao tutor.</summary>
public enum PreenchidoPor { Veterinario, Clinica }

public enum TipoAtendimento { Consulta, Acompanhamento }

public enum StatusSolicitacao { Recebida, AguardandoTutor, Autorizada, Agendada, Atendida, Cancelada }
