
using UnityEngine;

public interface ISkill
{
    // Método para iniciar o uso/mira da habilidade
    void AtivarHabilidade();

    // Método para cancelar o uso/mira e limpar efeitos
    void CancelarHabilidade();

    // Pergunta se a habilidade está fora do tempo de recarga (cooldown)
    bool EstaPronta();
}