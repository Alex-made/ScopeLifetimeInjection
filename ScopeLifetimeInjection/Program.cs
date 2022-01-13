using System;
using System.Collections.Generic;
using DuckFeeding;
using Microsoft.Extensions.DependencyInjection;

/* Это происходит в Startup */
var provider = new ServiceCollection()
    .AddTransient<List<IFood>>() //резолвится каждый раз
    .AddScoped<Duck>() //резолвится одна и та же сущность в рамках скоупа

    /* Фабричный метод для получения инстанса утки в скоупе */
    .AddTransient<IFeed>(s => s.GetRequiredService<Duck>())
    .AddTransient<IDuck>(s => s.GetRequiredService<Duck>())
    .AddTransient<DuckFeeder>()
    .BuildServiceProvider();

/* Саша кормит уток в главном скоупе. Т.к. scope не определен - будет использоваться по умолчанию созданный, т.н. основной.
   Устанавливаем еду "Молоко". 
 */
var sasha = provider.GetRequiredService<DuckFeeder>();
sasha.Feed(new Milk());
sasha.Ask();/* 1. Milk */

/*
   Mia & Riley кормят уток в своем скоупе. Тут мы создаем новую область видимости scope, и в ней получаем свой отдельный провайдер,
   который будет все добавленные через .AddScoped() объекты создавать заново. Поэтому mia создаст новую уточку, 
   а riley - будет кормить созданную в этой области. sasha же продолжит кормить уточку из своей области. 
*/
using (var duckFeedScope = provider.CreateScope())
{
    var mia = duckFeedScope.ServiceProvider.GetRequiredService<DuckFeeder>();
    mia.Feed(new Apple());
    mia.Ask();/* 2. Apple */

    var riley = duckFeedScope.ServiceProvider.GetRequiredService<DuckFeeder>();
    riley.Feed(new Banana());
    riley.Ask();/* 2. Apple, Banana */

    /* Данный скоуп ничего не значит для Sasha, он продолжит кормить утку в своем скоупе */
    sasha.Feed(new Milk());
    sasha.Ask();/* 1. Milk,Milk */
}

//  Result output
//  #1. Milk { }                    <- Sasha
//  #2. Apple { }                       <- Mia, From scope #2
//  #2. Apple { }, Banana { }           <- Riley, From scope #2
//  #1. Milk { }, Milk { }           <- Sasha, from scope #1
//  Disposed:#2. Count: 2               <- Disposed, From scope #2

/* Implementation */
namespace DuckFeeding
{
    /// <summary>
    /// Представляет кормителя уток.
    /// </summary>
    internal record DuckFeeder(IFeed Feeded, IDuck Duck)
    {
        /// <summary>
        /// Кормить.
        /// </summary>
        /// <param name="food"></param>
        public void Feed(IFood food) => Feeded.Eat(food);

        /// <summary>
        /// Крякнуть.
        /// </summary>
        public void Ask() => Duck.Quack();
    }

    /// <summary>
    /// Представляет еду.
    /// </summary>
	public interface IFeed
	{
        /// <summary>
        /// Есть.
        /// </summary>
        /// <param name="food">Еда.</param>
		void Eat(IFood food);
	}
    public interface IFood { }
    internal record Apple() : IFood;
    internal record Banana() : IFood;
    internal record Milk() : IFood;

	public interface IDuck
	{
		void Quack();
	}
    internal record Duck(List<IFood> Foods) : IDuck, IFeed, IDisposable
    {
        private bool _disposed = false;
        private static uint Id { get; set; }
        private string Name { get; } = "Утка " + ++Id + ".";
        public void Quack() => Console.WriteLine("#{0} {1}", Name, string.Join(", съела: ", Foods));
        public void Eat(IFood food) => Foods.Add(food);
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Console.WriteLine("Disposed:#{0} Count: {1}", Name, Foods.Count);
            Foods.Clear();
            _disposed = true;
        }
    }
}
