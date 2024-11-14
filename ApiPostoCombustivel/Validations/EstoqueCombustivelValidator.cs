﻿namespace ApiPostoCombustivel.Validations
{
    public class EstoqueCombustivelValidator
    {

        public static void ValidarEstoque(double? estoque)
        {
            if (estoque <= 0)
            {
                throw new InvalidOperationException("O estoque deve ser maior que zero.");
            }
        }

        public static void ValidarEstoqueSuficiente(double estoque, double quantidadeNecessaria)
        {
            if (estoque < quantidadeNecessaria)
            {
                throw new InvalidOperationException("Estoque insuficiente para realizar a operação.");
            }
        }
    }
}
