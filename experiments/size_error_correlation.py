import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import argparse
import os
from scipy.stats import pearsonr, spearmanr

def analyze_advanced_metrics(csv_path, output_dir):
    os.makedirs(output_dir, exist_ok=True)

    try:
        df = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error while reading file: {e}")
        return

    required_cols = ['FileName', 'Algorithm', 'VertexCount', 'TotalDistance', 'TotalElapsedMiliseconds']
    if not all(col in df.columns for col in required_cols):
        print("Error: Missing required columns in the CSV file.")
        return

    pattern = r'o(\d+)\.txt'
    opt_costs = df['FileName'].str.extract(pattern, expand=False)
    
    if opt_costs.isnull().any():
        df = df.dropna(subset=['FileName']) 

    df['OptimalDistance'] = opt_costs.astype(int)
    df['RelativeError'] = ((df['TotalDistance'] - df['OptimalDistance']) / df['OptimalDistance']) * 100

    algorithms = sorted(df['Algorithm'].unique())
    colors = plt.cm.get_cmap('tab10', len(algorithms))

    print("--- Correlation Analysis: Error vs. Problem Size ---")
    
    plt.figure(figsize=(10, 6))
    
    for idx, algo in enumerate(algorithms):
        algo_data = df[df['Algorithm'] == algo].sort_values(by='VertexCount')
        x = algo_data['VertexCount'].values
        y = algo_data['RelativeError'].values
        
        if len(x) > 1:
            pearson_corr, _ = pearsonr(x, y)
            spearman_corr, _ = spearmanr(x, y)
            print(f"Algorithm: {algo}")
            print(f"  Pearson correlation (linear):    {pearson_corr:.4f}")
            print(f"  Spearman correlation (monotonic): {spearman_corr:.4f}")
        
        plt.scatter(x, y, label=algo, color=colors(idx), alpha=0.7, s=40)
        
        if len(x) > 1:
            z = np.polyfit(x, y, 1)
            p = np.poly1d(z)
            plt.plot(x, p(x), color=colors(idx), linestyle='--', linewidth=2, alpha=0.8)

    print("-" * 50)
    plt.xlabel("Liczba wierzchołków", fontsize=12)
    plt.ylabel("Procentowy błąd względny", fontsize=12)
    plt.grid(True, linestyle='--', alpha=0.6)
    plt.legend(title='Algorytm')
    
    corr_path = os.path.join(output_dir, "correlation_plot.png")
    plt.savefig(corr_path, dpi=300, bbox_inches='tight')
    plt.close()

    plt.figure(figsize=(10, 6))
    
    for idx, algo in enumerate(algorithms):
        algo_data = df[df['Algorithm'] == algo]
        
        x_time = algo_data['TotalElapsedMiliseconds'].values
        y_error = algo_data['RelativeError'].values
        
        plt.scatter(x_time, y_error, label=algo, color=colors(idx), alpha=0.7, s=50)

    plt.xscale('log')
    
    plt.xlabel("Czas wykonania w milisekundach", fontsize=12)
    plt.ylabel("Procentowy błąd względny", fontsize=12)
    plt.grid(True, which="both", linestyle='--', alpha=0.4)
    plt.legend(title='Algorytm')
    
    tradeoff_path = os.path.join(output_dir, "tradeoff_plot.png")
    plt.savefig(tradeoff_path, dpi=300, bbox_inches='tight')
    plt.close()

def main():
    parser = argparse.ArgumentParser(description="Advanced Analysis (Trade-off & Correlation) for TSP")
    parser.add_argument("-i", "--input", required=True, help="Path to the CSV file with results")
    parser.add_argument("--out", default="plots", help="Target directory for generated plots")

    args = parser.parse_args()
    analyze_advanced_metrics(args.input, args.out)

if __name__ == "__main__":
    main()