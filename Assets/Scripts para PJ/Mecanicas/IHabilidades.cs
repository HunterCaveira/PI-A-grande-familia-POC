
using UnityEngine;

public interface IHabilidade
{
    // Método chamado pelo gerenciador para ativar o modo/função da habilidade
    void AtivarHabilidade();

    // Método chamado pelo gerenciador para cancelar a habilidade (ex: fechar fantasma)
    void CancelarHabilidade();
}