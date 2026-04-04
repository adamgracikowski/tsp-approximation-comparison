import pandas as pd
import matplotlib.pyplot as plt
import argparse
import os
from scipy.stats import wilcoxon

def analyze_relative_error(csv_path, output_dir):
    os.makedirs(output_dir, exist_ok=True)

    try:
        df = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error while reading file: {e}")
        return

    required_cols = ['FileName', 'Algorithm', 'TotalDistance']
    if not all(col in df.columns for col in required_cols):
        print("Error: Missing columns FileName, Algorithm or TotalDistance.")
        return

    pattern = r'o(\d+)\.txt'
    opt_costs = df['FileName'].str.extract(pattern, expand=False)
    
    if opt_costs.isnull().any():
        df = df.dropna(subset=['FileName']) 

    df['OptimalDistance'] = opt_costs.astype(int)
    df['RelativeError'] = ((df['TotalDistance'] - df['OptimalDistance']) / df['OptimalDistance']) * 100

    algorithms = sorted(df['Algorithm'].unique())

    stats = df.groupby('Algorithm')['RelativeError'].agg(['count', 'mean', 'median', 'min', 'max', 'std']).reset_index()
    stats.rename(columns={
        'mean': 'Average Error [%]', 
        'median': 'Median Error [%]',
        'std': 'Standard Deviation'
    }, inplace=True)

    print("Statistical Summary (Relative Error %):")
    print(stats.to_string(index=False))
    print("-" * 50)

    if len(algorithms) == 2:
        algo1, algo2 = algorithms[0], algorithms[1]
        
        data1 = df[df['Algorithm'] == algo1][['FileName', 'RelativeError']]
        data2 = df[df['Algorithm'] == algo2][['FileName', 'RelativeError']]
        
        merged = pd.merge(data1, data2, on='FileName', suffixes=(f'_{algo1}', f'_{algo2}'))
        
        stat, p_value = wilcoxon(merged[f'RelativeError_{algo1}'], merged[f'RelativeError_{algo2}'], zero_method='zsplit')
        
        print("\n--- Wilcoxon Signed-Rank Test ---")
        print(f"Compared algorithms: {algo1} vs {algo2}")
        print(f"Number of shared instances analyzed: {len(merged)}")
        print(f"Test statistic (W): {stat:.4f}")
        print(f"p-value: {p_value:.4e}")
        
        alpha = 0.05
        print("\nConclusion:")
        if p_value < alpha:
            print(f"We reject the null hypothesis (p < {alpha}).")
            print("The difference in quality between the algorithms is statistically significant.")
        else:
            print(f"There is no significant difference between the algorithms (p >= {alpha}).")
            print("The difference in quality between the algorithms is NOT statistically significant (it may be due to chance).")
        print("-" * 50)

    plt.figure(figsize=(10, 6))
    
    boxplot_data = [df[df['Algorithm'] == algo]['RelativeError'].values for algo in algorithms]
    
    plt.boxplot(boxplot_data, tick_labels=[f"{algo}" for algo in algorithms])
    
    plt.xlabel("Algorytm")
    plt.ylabel("Procentowy błąd względny")
    plt.grid(axis='y', linestyle='--', alpha=0.7)
    
    box_path = os.path.join(output_dir, "relative_error_boxplot.png")
    plt.savefig(box_path, dpi=300, bbox_inches='tight')
    plt.close()

def main():
    parser = argparse.ArgumentParser(description="Statistical Analysis of Relative Error in TSP")
    parser.add_argument("-i", "--input", required=True, help="Path to the CSV file with results")
    parser.add_argument("--out", default="plots", help="Target directory for generated plots")

    args = parser.parse_args()
    
    analyze_relative_error(args.input, args.out)

if __name__ == "__main__":
    main()