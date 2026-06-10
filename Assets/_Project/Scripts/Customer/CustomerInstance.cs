using UnityEngine;
using Nyangsta.Data;

namespace Nyangsta.Customer
{
    /// <summary>
    /// Runtime state for one seated customer. Pure data + a tick that advances
    /// the FSM; serving logic lives in CustomerManager.
    /// </summary>
    public class CustomerInstance
    {
        public CustomerData Data { get; }
        public CustomerState State { get; private set; }
        public float PatienceRemaining { get; private set; }
        public float EatTimer { get; private set; }

        /// <summary>Seat this customer occupies; assigned by CustomerManager on spawn.</summary>
        public int SeatIndex { get; }

        /// <summary>0..1 patience remaining, for view bars.</summary>
        public float PatienceNormalized =>
            Data.patienceSeconds > 0 ? Mathf.Clamp01(PatienceRemaining / Data.patienceSeconds) : 0f;

        private const float EnterDuration = 0.5f;
        private const float EatDuration = 2f;
        private const float PayDuration = 0.5f;
        private float _stateTimer;

        public CustomerInstance(CustomerData data, int seatIndex)
        {
            Data = data;
            SeatIndex = seatIndex;
            State = CustomerState.Entering;
            PatienceRemaining = data.patienceSeconds;
            _stateTimer = EnterDuration;
        }

        public bool IsFinished => State == CustomerState.Leaving && _stateTimer <= 0f;
        public bool LeftAngry { get; private set; }

        /// <summary>Advances time-based transitions. Returns true when fully done.</summary>
        public bool Tick(float dt)
        {
            switch (State)
            {
                case CustomerState.Entering:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) { State = CustomerState.Waiting; }
                    break;

                case CustomerState.Waiting:
                case CustomerState.Ordering:
                    PatienceRemaining -= dt;
                    if (PatienceRemaining <= 0f) BeginLeaving(angry: true);
                    break;

                case CustomerState.Eating:
                    EatTimer -= dt;
                    if (EatTimer <= 0f) State = CustomerState.Paying;
                    break;

                case CustomerState.Paying:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f) BeginLeaving(angry: false);
                    break;

                case CustomerState.Leaving:
                    _stateTimer -= dt;
                    break;
            }
            return IsFinished;
        }

        /// <summary>Serve a menu. Returns the gold paid (0 if it can't be served now).</summary>
        public int Serve(MenuData menu, float revenueMultiplier = 1f)
        {
            if (State != CustomerState.Waiting && State != CustomerState.Ordering) return 0;

            float multiplier = (Data.preferredMenu == menu) ? Data.payMultiplier : 1f;
            int paid = Mathf.RoundToInt(menu.sellPrice * multiplier * Mathf.Max(0f, revenueMultiplier));

            State = CustomerState.Eating;
            EatTimer = EatDuration;
            return paid;
        }

        private void BeginLeaving(bool angry)
        {
            LeftAngry = angry;
            State = CustomerState.Leaving;
            _stateTimer = PayDuration;
        }
    }
}
